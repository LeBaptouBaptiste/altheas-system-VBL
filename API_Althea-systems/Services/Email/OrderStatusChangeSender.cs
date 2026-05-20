using System.Globalization;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services.Email;

/// <summary>
/// Phase 5: notifies the customer when their order moves through the major
/// fulfilment milestones (Shipped, Delivered). Smaller status hops
/// (Pending → Confirmed → Processing) are intentionally silent — those are
/// internal admin steps that would just generate noise in the customer's
/// inbox.
/// </summary>
public interface IOrderStatusChangeSender
{
    /// <summary>
    /// Sends the right template for <paramref name="newStatus"/>. Returns
    /// without sending for statuses we don't notify on (everything except
    /// Shipped and Delivered).
    /// </summary>
    Task SendAsync(Order order, User user, OrderStatus newStatus, CancellationToken ct = default);
}

public class OrderStatusChangeSender : IOrderStatusChangeSender
{
    private static readonly CultureInfo FrFr = new("fr-FR");

    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly ILogger<OrderStatusChangeSender> _logger;

    public OrderStatusChangeSender(
        IEmailSender emailSender,
        IEmailTemplateRenderer renderer,
        ILogger<OrderStatusChangeSender> logger)
    {
        _emailSender = emailSender;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task SendAsync(Order order, User user, OrderStatus newStatus, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(user);

        // Allowlist — explicit so adding a new milestone is a deliberate
        // change here, not an accident upstream.
        var (templateName, subject) = newStatus switch
        {
            OrderStatus.Shipped => ("order-shipped",
                $"Votre commande #{ShortId(order.Id)} a été expédiée — Althea Systems"),
            OrderStatus.Delivered => ("order-delivered",
                $"Votre commande #{ShortId(order.Id)} a été livrée — Althea Systems"),
            _ => (null, null),
        };

        if (templateName is null)
        {
            _logger.LogDebug(
                "OrderStatusChangeSender: status {Status} is not customer-facing — skipping.",
                newStatus);
            return;
        }

        var firstName = string.IsNullOrWhiteSpace(user.Name)
            ? user.Email.Split('@')[0]
            : user.Name.Split(' ')[0];

        var html = _renderer.Render(templateName, new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["orderShortId"] = ShortId(order.Id),
            ["orderDate"] = order.Date.ToString("dd MMMM yyyy", FrFr),
            ["itemCount"] = order.Items.Count.ToString(),
            // Shipping address is the relevant one in shipped/delivered
            // notifications — defensive null check.
            ["shippingCity"] = order.ShippingAddress?.City ?? "",
            ["shippingPostalCode"] = order.ShippingAddress?.PostalCode ?? "",
        });

        await _emailSender.SendAsync(
            to: user.Email,
            subject: subject!,
            htmlBody: html,
            ct: ct);

        _logger.LogInformation(
            "Order-{Status} email sent for order {OrderId} to {Email}.",
            newStatus, order.Id, user.Email);
    }

    private static string ShortId(Guid id) => id.ToString("N")[..8].ToUpperInvariant();
}
