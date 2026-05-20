using API_Althea_systems.Services.IServices;
using Stripe;

namespace API_Althea_systems.Services;

public class StripeRefundService : IStripeRefundService
{
    private readonly RefundService _refunds;
    private readonly ILogger<StripeRefundService> _logger;

    public StripeRefundService(ILogger<StripeRefundService> logger)
        : this(new RefundService(), logger) { }

    // Test seam: lets unit tests inject a mocked RefundService without
    // touching the network.
    internal StripeRefundService(RefundService refunds, ILogger<StripeRefundService> logger)
    {
        _refunds = refunds;
        _logger = logger;
    }

    public async Task<(string RefundId, string Status)> RefundAsync(
        string paymentIntentId,
        long amountCents,
        string? reason = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);
        if (amountCents <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountCents),
                "Refund amount must be positive.");

        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = amountCents,
            Reason = reason,
        };

        var refund = await _refunds.CreateAsync(options, cancellationToken: ct);

        _logger.LogInformation(
            "Stripe refund {RefundId} created on {IntentId} for {Amount} cents (status={Status}).",
            refund.Id, paymentIntentId, amountCents, refund.Status);

        return (refund.Id, refund.Status);
    }
}
