using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;
using Microsoft.Extensions.Options;
using Stripe;

namespace API_Althea_systems.Services;

/// <summary>
/// Stripe SDK wrapper. The amount of a PaymentIntent is computed
/// server-side from the order's line items + shipping, never from
/// the client — see <see cref="CreatePaymentIntentAsync"/>.
/// </summary>
public class StripeService : IStripeService
{
    private const string Currency = "eur";

    private readonly CustomerService _customers;
    private readonly PaymentIntentService _paymentIntents;
    private readonly PaymentMethodService _paymentMethods;
    private readonly IUserRepository _userRepository;
    private readonly IVatCalculationService _vat;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IOptions<StripeOptions> options,
        IUserRepository userRepository,
        IVatCalculationService vat,
        ILogger<StripeService> logger)
    {
        var secretKey = options.Value.SecretKey
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");

        // Single StripeClient instance is fine to construct per-service-resolution
        // (StripeService is scoped); under the hood it just wraps HttpClient.
        var stripeClient = new StripeClient(secretKey);
        _customers = new CustomerService(stripeClient);
        _paymentIntents = new PaymentIntentService(stripeClient);
        _paymentMethods = new PaymentMethodService(stripeClient);

        _userRepository = userRepository;
        _vat = vat;
        _logger = logger;
    }

    public async Task<string> GetOrCreateCustomerAsync(User user, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(user.StripeCustomerId))
            return user.StripeCustomerId;

        var customer = await _customers.CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.Name,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = user.Id.ToString(),
            },
        }, cancellationToken: ct);

        user.StripeCustomerId = customer.Id;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}",
            customer.Id, user.Id);

        return customer.Id;
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        Order order,
        User user,
        bool saveCard,
        CancellationToken ct = default)
    {
        var customerId = await GetOrCreateCustomerAsync(user, ct);

        // AUTHORITATIVE amount: compute from items + shipping, never trust client.
        var totals = _vat.CalculateOrderTotals(
            order.Items.Select(i => (i.PriceHT, i.Quantity, i.VatRate)),
            order.ShippingCost);

        // Stripe expects the smallest currency unit (cents for EUR).
        var amountInCents = (long)Math.Round(totals.TotalTTC * 100m);

        if (amountInCents <= 0)
            throw new InvalidOperationException(
                $"Order {order.Id} has a non-positive total ({totals.TotalTTC} EUR); refusing to create a PaymentIntent.");

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = Currency,
            Customer = customerId,
            // Modern Stripe flow: we don't enumerate payment_method_types
            // (let Stripe pick from the customer's Dashboard config + automatic
            // payment methods). Cards are always enabled by default.
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            // off_session means the card can be charged later WITHOUT user
            // interaction. Stripe will ask for 3DS at the first charge if needed,
            // then the saved method works for one-click checkout afterwards.
            SetupFutureUsage = saveCard ? "off_session" : null,
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString(),
                ["userId"] = user.Id.ToString(),
            },
            Description = $"Althea Systems order {order.Id}",
        };

        var intent = await _paymentIntents.CreateAsync(options, cancellationToken: ct);

        _logger.LogInformation(
            "Created PaymentIntent {IntentId} ({Amount} {Currency}) for order {OrderId}, user {UserId}, saveCard={SaveCard}",
            intent.Id, intent.Amount, intent.Currency, order.Id, user.Id, saveCard);

        return intent;
    }

    public async Task DetachPaymentMethodAsync(string stripePaymentMethodId, CancellationToken ct = default)
    {
        try
        {
            await _paymentMethods.DetachAsync(stripePaymentMethodId, cancellationToken: ct);
            _logger.LogInformation("Detached Stripe PaymentMethod {MethodId}", stripePaymentMethodId);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            // Already detached on Stripe's side (manual dashboard cleanup, prior
            // call that crashed after Stripe but before our DB delete, …) — the
            // user's intent is satisfied either way.
            _logger.LogInformation(
                "PaymentMethod {MethodId} was already detached on Stripe — treating as no-op.",
                stripePaymentMethodId);
        }
    }
}

/// <summary>
/// Strongly-typed binding for the "Stripe" section of configuration.
/// Loaded via <see cref="IOptions{TOptions}"/>; validation happens at
/// startup in ServiceCollectionExtensions.
/// </summary>
public class StripeOptions
{
    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string? ApiVersion { get; set; }
}
