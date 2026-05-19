using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using API_Althea_systems.Data;
using API_Althea_systems.Models.Payments;
using API_Althea_systems.Services;
using Stripe;

namespace API_Althea_systems.Controllers;

/// <summary>
/// Inbound Stripe webhook endpoint. Stripe POSTs anonymously here when
/// payment events happen on its side (succeeded, failed, refunded, …) and
/// we use it to settle the order server-side — the frontend confirmation
/// path is only for UX, the source of truth is this webhook.
///
/// SECURITY: there is NO [Authorize] guard. Stripe authenticates itself
/// via a HMAC signature over the raw request body using a shared secret
/// (Stripe:WebhookSecret). Anyone POSTing without a valid signature gets
/// 400 — without that check, a random attacker could mark any order as
/// paid by curling this endpoint.
///
/// IDEMPOTENCY: Stripe redelivers webhooks on transient errors and supports
/// manual replay from the dashboard. We persist every event_id and skip
/// duplicates. See Models/Payments/WebhookEvent.
///
/// DISPATCH: this controller verifies + records the event then logs.
/// Per-event handlers (payment_intent.succeeded → Order update) land in
/// the next commit.
/// </summary>
[ApiController]
[Route("api/webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeOptions _options;
    private readonly AltheaDbContext _db;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOptions<StripeOptions> options,
        AltheaDbContext db,
        ILogger<StripeWebhookController> logger)
    {
        _options = options.Value;
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        // 1. Read the raw body — Stripe's HMAC is computed over the bytes,
        //    so any re-serialization (which automatic [FromBody] would do)
        //    breaks the signature. No [FromBody] parameter on the action
        //    means ASP.NET leaves Request.Body untouched.
        string json;
        using (var reader = new StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        // 2. Refuse to run if the webhook secret is unconfigured / placeholder.
        //    In dev this surfaces immediately instead of silently accepting
        //    spoofed events. In prod, fail-fast at startup would be ideal but
        //    that's the wider Stripe:WebhookSecret validation, not this endpoint.
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret)
            || _options.WebhookSecret.Contains("REPLACE_ME"))
        {
            _logger.LogError("Stripe webhook received but Stripe:WebhookSecret is not configured.");
            return StatusCode(500);
        }

        // 3. Verify the signature. EventUtility throws StripeException on
        //    any mismatch (wrong secret, replayed timestamp, tampered body).
        //    We return 400 without leaking the reason — Stripe's convention.
        Event stripeEvent;
        try
        {
            var signature = Request.Headers["Stripe-Signature"].ToString();
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _options.WebhookSecret,
                // Default tolerance is 5 minutes. Tight enough to limit replays.
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return BadRequest();
        }

        // 4. Idempotency: have we already processed this event_id?
        //    There's a theoretical race when Stripe redelivers concurrently,
        //    but per-event handlers themselves stay idempotent (they re-check
        //    the order's current state before mutating) so a double-execution
        //    has no business impact.
        if (await _db.WebhookEvents.AnyAsync(w => w.EventId == stripeEvent.Id, ct))
        {
            _logger.LogInformation(
                "Duplicate Stripe webhook event {EventId} ({Type}), skipping.",
                stripeEvent.Id, stripeEvent.Type);
            return Ok();
        }

        _db.WebhookEvents.Add(new WebhookEvent
        {
            EventId = stripeEvent.Id,
            Type = stripeEvent.Type,
        });
        await _db.SaveChangesAsync(ct);

        // 5. Dispatch. Specific handlers come in the next commit; for now
        //    we log unhandled events so ops can see what Stripe is sending.
        switch (stripeEvent.Type)
        {
            // case "payment_intent.succeeded":
            // case "payment_intent.payment_failed":
            // case "payment_method.attached":
            //   → next commit
            default:
                _logger.LogInformation(
                    "Stripe webhook {Type} ({EventId}) accepted but no handler wired yet.",
                    stripeEvent.Type, stripeEvent.Id);
                break;
        }

        return Ok();
    }
}
