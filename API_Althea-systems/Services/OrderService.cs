using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Shared;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.Email;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderStatusChangeSender _statusChangeSender;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IOrderStatusChangeSender statusChangeSender,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _statusChangeSender = statusChangeSender;
        _logger = logger;
    }

    public async Task<OrderDto> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Order", id);
        return MapToDto(order);
    }

    public async Task<PaginatedResponse<OrderDto>> GetAllAsync(int page, int pageSize, Guid? userId = null)
    {
        var orders = await _orderRepository.GetAllAsync(page, pageSize, userId);
        var total = await _orderRepository.CountAsync(userId);
        return new PaginatedResponse<OrderDto>(
            orders.Select(MapToDto), page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<OrderDto> CreateAsync(Guid userId, OrderCreateRequest request)
    {
        var items = new List<OrderItem>();
        foreach (var itemReq in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemReq.ProductId)
                ?? throw new NotFoundException("Product", itemReq.ProductId);

            items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductNameFr = product.NameFr,
                ProductNameEn = product.NameEn,
                Quantity = itemReq.Quantity,
                PriceHT = product.PriceHT,
                VatRate = product.VatRate
            });
        }

        var shippingCost = GetShippingCost(request.ShippingMethod);

        // ── Phase 7: validate store credit usage ─────────
        // Authoritative total computation — never trust the client. Credit
        // must be ≤ user.CreditBalanceCents AND must leave at least 50 cents
        // (Stripe's minimum EUR charge) to clear via Stripe. Anything else
        // → reject with a precise reason.
        var creditApplied = request.CreditAppliedCents;
        if (creditApplied < 0)
        {
            throw new BadRequestException("Credit applied cannot be negative.",
                reason: "invalid_credit");
        }
        if (creditApplied > 0)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);
            if (creditApplied > user.CreditBalanceCents)
            {
                throw new BadRequestException(
                    $"Credit applied ({creditApplied} cents) exceeds available balance " +
                    $"({user.CreditBalanceCents} cents).",
                    reason: "insufficient_credit");
            }
            var totalHT = items.Sum(i => i.PriceHT * i.Quantity);
            var totalVAT = items.Sum(i => i.PriceHT * i.Quantity * GetVatMultiplier(i.VatRate));
            var totalTTCcents = (long)Math.Round((totalHT + totalVAT + shippingCost) * 100m);
            if (creditApplied >= totalTTCcents - 50)
            {
                // 50 cents = Stripe's minimum charge in EUR. We refuse rather
                // than auto-cap so the client knows exactly what landed.
                throw new BadRequestException(
                    $"Credit applied ({creditApplied} cents) would leave the Stripe charge below " +
                    "the 0.50 € minimum. Remove items or apply less credit.",
                    reason: "credit_too_large");
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = DateTime.UtcNow,
            BillingAddressId = request.BillingAddressId,
            ShippingAddressId = request.ShippingAddressId,
            ShippingMethod = request.ShippingMethod,
            PaymentMethod = request.PaymentMethod,
            ShippingCost = shippingCost,
            Items = items,
            CreditAppliedCents = creditApplied,
        };

        await _orderRepository.CreateAsync(order);
        return await GetByIdAsync(order.Id);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid id, Guid changedByUserId, OrderStatusUpdateRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Order", id);

        var statusChange = new OrderStatusChange
        {
            // Leave Id at default(Guid). OrderRepository.UpdateAsync calls
            // _context.Orders.Update(order) which cascades through navigation
            // collections — entities with a non-default PK get marked Modified
            // (UPDATE → 0 rows → DbUpdateConcurrencyException), default PKs get
            // marked Added (INSERT). Same trap we hit on Address / PaymentMethod.
            OrderId = order.Id,
            From = order.Status,
            To = request.Status,
            Date = DateTime.UtcNow,
            UserId = changedByUserId
        };

        order.StatusHistory.Add(statusChange);
        order.Status = request.Status;

        await _orderRepository.UpdateAsync(order);

        // Phase 5: fire customer notification AFTER the DB commit. Sender
        // allow-lists Shipped + Delivered internally — everything else is
        // a no-op there. Independent try/catch so a flaky SMTP doesn't
        // surface as a 500 to the admin who just clicked the status dropdown.
        try
        {
            if (order.User is not null)
            {
                await _statusChangeSender.SendAsync(order, order.User, request.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Order-status-change email failed for order {OrderId} → {Status}. " +
                "Status was committed; mail can be re-fired manually if needed.",
                order.Id, request.Status);
        }

        return await GetByIdAsync(order.Id);
    }

    private static decimal GetShippingCost(Common.Enums.ShippingMethod method) => method switch
    {
        Common.Enums.ShippingMethod.Standard => 15m,
        Common.Enums.ShippingMethod.Express => 35m,
        Common.Enums.ShippingMethod.Overnight => 75m,
        _ => 15m
    };

    private static OrderDto MapToDto(Order o)
    {
        var totalHT = o.Items.Sum(i => i.PriceHT * i.Quantity);
        var totalVAT = o.Items.Sum(i => i.PriceHT * i.Quantity * GetVatMultiplier(i.VatRate));

        // Latest "real" invoice (Type=Invoice, excluding credit notes) for this
        // order — null if none yet. Used by /account/orders to decide whether
        // to enable the download button. Repository must Include(o.Invoices)
        // for this to be populated (cf. OrderRepository.GetByIdAsync/GetAllAsync).
        var latestInvoiceId = o.Invoices
            .Where(inv => inv.Type == Models.Invoices.InvoiceType.Invoice)
            .OrderByDescending(inv => inv.Date)
            .Select(inv => (Guid?)inv.Id)
            .FirstOrDefault();

        // Phase 6: full list of invoices + credit notes attached to the order
        // so the client UI can offer one download per row. Same Include
        // dependency on OrderRepository as latestInvoiceId.
        var invoiceSummaries = o.Invoices
            .OrderBy(inv => inv.Date)
            .Select(inv => new OrderInvoiceSummaryDto(
                inv.Id,
                inv.Number,
                inv.Type,
                inv.Date,
                inv.AmountTTC,
                inv.Status,
                inv.RelatedInvoiceId))
            .ToList();

        return new OrderDto(
            o.Id, o.UserId, o.User?.Name ?? "", o.Date, o.Status, o.PaymentStatus,
            o.PaymentMethod,
            MapAddress(o.BillingAddress), MapAddress(o.ShippingAddress),
            o.ShippingMethod, o.ShippingCost, totalHT, totalVAT, totalHT + totalVAT + o.ShippingCost,
            o.CreatedAt, o.UpdatedAt,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductNameFr, i.ProductNameEn, i.Quantity, i.PriceHT, i.VatRate)),
            o.StatusHistory.Select(s => new OrderStatusChangeDto(s.From, s.To, s.Date, s.UserId)),
            latestInvoiceId,
            invoiceSummaries,
            o.CreditAppliedCents
        );
    }

    private static decimal GetVatMultiplier(Common.Enums.VatRate rate) => rate switch
    {
        Common.Enums.VatRate.Standard => 0.20m,
        Common.Enums.VatRate.Intermediate => 0.10m,
        Common.Enums.VatRate.Reduced => 0.055m,
        _ => 0m
    };

    private static AddressDto MapAddress(Models.Users.Address? a) =>
        a == null ? new AddressDto(Guid.Empty, "", "", "", null, "", null, "", "", "", null)
        : new AddressDto(a.Id, a.Label, a.FirstName, a.LastName, a.Company, a.Street, a.Street2, a.City, a.PostalCode, a.Country, a.Phone);
}
