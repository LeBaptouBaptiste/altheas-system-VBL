using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using API_Althea_systems.Controllers;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Analytics;
using API_Althea_systems.Services;
using API_Althea_systems.Services.IServices;
using StripeApi = Stripe;

namespace API_Althea_systems.Tests.Controllers;

/// <summary>
/// Integration-flavored tests for the webhook endpoint. These exercise the
/// controller as a whole (raw body reading, Stripe signature verification,
/// idempotency via the WebhookEvents table) with a real in-memory EF context
/// and a mocked IStripeWebhookProcessor.
///
/// What we're guarding against:
/// - A regression that re-introduces [FromBody] (Stripe signature breaks).
/// - A signature bypass (any 200 on tampered payload is a critical bug).
/// - Lost idempotency (Stripe redelivers, we double-charge / re-confirm orders).
/// </summary>
/// <summary>
/// Test-only context that ignores entities the InMemory provider can't map.
/// SalesAnalytics uses a Dictionary&lt;string,decimal&gt; persisted as JSONB on
/// Postgres — perfectly valid in prod, but InMemory doesn't understand it
/// and throws at model validation. We don't touch analytics in webhook
/// tests, so dropping the entity entirely is the cleanest workaround.
/// </summary>
internal class TestAltheaDbContext : AltheaDbContext
{
    public TestAltheaDbContext(DbContextOptions<AltheaDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<SalesAnalytics>();
    }
}

public class StripeWebhookControllerTests : IDisposable
{
    private const string WebhookSecret = "whsec_test_super_secret_for_unit_tests";

    private readonly AltheaDbContext _db;
    private readonly Mock<IStripeWebhookProcessor> _processor = new();
    private readonly StripeWebhookController _sut;

    public StripeWebhookControllerTests()
    {
        var options = new DbContextOptionsBuilder<AltheaDbContext>()
            .UseInMemoryDatabase($"webhook-tests-{Guid.NewGuid()}")
            .Options;
        _db = new TestAltheaDbContext(options);

        var stripeOptions = Options.Create(new StripeOptions
        {
            SecretKey = "sk_test_irrelevant_here",
            WebhookSecret = WebhookSecret,
        });

        _sut = new StripeWebhookController(
            stripeOptions, _db, _processor.Object, NullLogger<StripeWebhookController>.Instance);
    }

    public void Dispose() => _db.Dispose();

    /// <summary>
    /// Builds a minimal Stripe event JSON. We don't validate the shape against
    /// Stripe's schema — that's the SDK's job — we just need a valid JSON
    /// document with an `id` and `type` for the controller to extract.
    /// </summary>
    private static string MakeEventJson(string eventId, string type) =>
        // Minimal real-shape Stripe event payload. The api_version is REQUIRED
        // even in tests: Stripe's EventConverter reads it to route data.object
        // to the right concrete model; without it, ReadJson NREs.
        // data.object also needs its own "object" discriminator (payment_intent).
        $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2024-12-18.acacia",
          "type": "{{type}}",
          "data": { "object": { "id": "pi_test_irrelevant", "object": "payment_intent" } },
          "created": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
          "livemode": false,
          "pending_webhooks": 0,
          "request": null
        }
        """;

    /// <summary>
    /// Reproduces Stripe's signature header format
    /// (https://stripe.com/docs/webhooks/signatures#verify-manually) so we can
    /// craft a request that EventUtility.ConstructEvent will accept.
    /// </summary>
    private static string SignPayload(string payload, string secret, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{ts}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var sigHex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={ts},v1={sigHex}";
    }

    /// <summary>Builds a controller context whose Request.Body streams the given payload.</summary>
    private static void SetRequestBody(StripeWebhookController controller, string body, string? signature)
    {
        var http = new DefaultHttpContext();
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        http.Request.ContentType = "application/json";
        if (signature is not null)
            http.Request.Headers["Stripe-Signature"] = signature;
        controller.ControllerContext = new ControllerContext { HttpContext = http };
    }

    // ── Signature path ────────────────────────────────────────────

    [Fact]
    public async Task Handle_InvalidSignature_Returns400_NoDbWrite_NoDispatch()
    {
        var payload = MakeEventJson("evt_bad", "payment_intent.succeeded");
        SetRequestBody(_sut, payload, signature: "t=123,v1=deadbeef");

        var result = await _sut.Handle(default);

        result.Should().BeOfType<BadRequestResult>();
        (await _db.WebhookEvents.CountAsync()).Should().Be(0, "no event should be persisted on bad signature");
        _processor.Verify(p => p.ProcessAsync(It.IsAny<StripeApi.Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TamperedPayload_ButValidSigOnOriginal_Returns400()
    {
        // The classic attack : signature was computed for one payload, then
        // the attacker swapped the body. EventUtility re-hashes and rejects.
        var original = MakeEventJson("evt_legit", "payment_intent.succeeded");
        var validSignature = SignPayload(original, WebhookSecret);
        var tampered = MakeEventJson("evt_attack", "payment_intent.succeeded");

        SetRequestBody(_sut, tampered, validSignature);

        var result = await _sut.Handle(default);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Handle_MissingSignatureHeader_Returns400()
    {
        var payload = MakeEventJson("evt_no_sig", "payment_intent.succeeded");
        SetRequestBody(_sut, payload, signature: null);

        var result = await _sut.Handle(default);

        result.Should().BeOfType<BadRequestResult>();
    }

    // ── Webhook secret config ─────────────────────────────────────

    [Fact]
    public async Task Handle_UnconfiguredSecret_Returns500()
    {
        // Fresh controller with placeholder secret — refuses to run.
        var stripeOptions = Options.Create(new StripeOptions
        {
            SecretKey = "sk_test_x",
            WebhookSecret = "whsec_REPLACE_ME",
        });
        var sut = new StripeWebhookController(
            stripeOptions, _db, _processor.Object, NullLogger<StripeWebhookController>.Instance);

        var payload = MakeEventJson("evt_x", "payment_intent.succeeded");
        SetRequestBody(sut, payload, "t=1,v1=x");

        var result = await sut.Handle(default);

        var status = result.Should().BeOfType<StatusCodeResult>().Subject;
        status.StatusCode.Should().Be(500);
    }

    // ── Happy path + idempotency ──────────────────────────────────

    [Fact]
    public async Task Handle_ValidNewEvent_Returns200_PersistsAndDispatches()
    {
        var payload = MakeEventJson("evt_new", "payment_intent.succeeded");
        SetRequestBody(_sut, payload, SignPayload(payload, WebhookSecret));

        var result = await _sut.Handle(default);

        result.Should().BeOfType<OkResult>();
        var saved = await _db.WebhookEvents.SingleAsync();
        saved.EventId.Should().Be("evt_new");
        saved.Type.Should().Be("payment_intent.succeeded");
        _processor.Verify(p => p.ProcessAsync(
            It.Is<StripeApi.Event>(e => e.Id == "evt_new"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEvent_Returns200_ButDoesNotReprocess()
    {
        // First delivery
        var payload = MakeEventJson("evt_dup", "payment_intent.succeeded");
        SetRequestBody(_sut, payload, SignPayload(payload, WebhookSecret));
        await _sut.Handle(default);

        // Stripe redelivers the same event — sign again (fresh timestamp is fine,
        // signature still valid because EventUtility checks the signed payload).
        SetRequestBody(_sut, payload, SignPayload(payload, WebhookSecret));
        var result = await _sut.Handle(default);

        result.Should().BeOfType<OkResult>();
        (await _db.WebhookEvents.CountAsync()).Should().Be(1, "the same event_id must not be inserted twice");
        // Processor called only on first delivery, not on the redelivery.
        _processor.Verify(p => p.ProcessAsync(It.IsAny<StripeApi.Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
