using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services;
using API_Althea_systems.Services.Email;

namespace API_Althea_systems.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IOrderStatusChangeSender> _statusSender = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(
            _orderRepo.Object,
            _productRepo.Object,
            _statusSender.Object,
            NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsDto()
    {
        var order = CreateTestOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.GetByIdAsync(order.Id);

        result.Id.Should().Be(order.Id);
        result.TotalHT.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ThrowsNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesOrder()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(), NameFr = "Test", NameEn = "Test",
            PriceHT = 100m, VatRate = VatRate.Standard,
            Slug = "t", DescriptionFr = "", DescriptionEn = "",
            LongDescriptionFr = "", LongDescriptionEn = "", Images = []
        };
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var createdOrder = CreateTestOrder();
        _orderRepo.Setup(r => r.CreateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(createdOrder);

        var request = new OrderCreateRequest(
            Guid.NewGuid(), Guid.NewGuid(),
            ShippingMethod.Standard, PaymentMethod.Card,
            [new OrderItemCreateRequest(product.Id, 2)]
        );

        var result = await _sut.CreateAsync(Guid.NewGuid(), request);

        result.Should().NotBeNull();
        _orderRepo.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidOrder_UpdatesStatus()
    {
        var order = CreateTestOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.UpdateStatusAsync(order.Id, Guid.NewGuid(), new OrderStatusUpdateRequest(OrderStatus.Shipped));

        order.Status.Should().Be(OrderStatus.Shipped);
        order.StatusHistory.Should().HaveCount(1);
    }

    // ─────────────────────────────────────────────────────────
    //  Phase 5 — status-change notifications
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ToShipped_FiresShippedNotification()
    {
        var order = CreateTestOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        await _sut.UpdateStatusAsync(order.Id, Guid.NewGuid(), new OrderStatusUpdateRequest(OrderStatus.Shipped));

        _statusSender.Verify(s => s.SendAsync(order, order.User, OrderStatus.Shipped,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToDelivered_FiresDeliveredNotification()
    {
        var order = CreateTestOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        await _sut.UpdateStatusAsync(order.Id, Guid.NewGuid(), new OrderStatusUpdateRequest(OrderStatus.Delivered));

        _statusSender.Verify(s => s.SendAsync(order, order.User, OrderStatus.Delivered,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToProcessing_StillCallsSender_WhichAllowListsAtItsLayer()
    {
        // OrderService is dumb on which statuses notify — the allow-list
        // lives in OrderStatusChangeSender. Verify the call happens; the
        // sender's own tests cover the no-op branch.
        var order = CreateTestOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        await _sut.UpdateStatusAsync(order.Id, Guid.NewGuid(), new OrderStatusUpdateRequest(OrderStatus.Processing));

        _statusSender.Verify(s => s.SendAsync(order, order.User, OrderStatus.Processing,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_NotificationFails_DoesNotRollBackStatus()
    {
        // Admin clicked the status dropdown — committing the DB change is
        // what they expect. A flaky SMTP must not surface as a 500 that
        // makes them think the status change failed.
        var order = CreateTestOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _statusSender
            .Setup(s => s.SendAsync(It.IsAny<Order>(), It.IsAny<User>(),
                It.IsAny<OrderStatus>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("smtp down", new Exception()));

        var act = () => _sut.UpdateStatusAsync(order.Id, Guid.NewGuid(),
            new OrderStatusUpdateRequest(OrderStatus.Shipped));

        await act.Should().NotThrowAsync();
        order.Status.Should().Be(OrderStatus.Shipped);
        _orderRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    private static Order CreateTestOrder()
    {
        var address = new Address
        {
            Id = Guid.NewGuid(), Label = "Test", FirstName = "A", LastName = "B",
            Street = "1 rue", City = "Paris", PostalCode = "75001", Country = "France"
        };

        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Name = "Test", Email = "t@t.com", PasswordHash = "", Addresses = [], PaymentMethods = [] },
            Date = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.Card,
            ShippingMethod = ShippingMethod.Standard,
            ShippingCost = 15m,
            BillingAddress = address,
            ShippingAddress = address,
            BillingAddressId = address.Id,
            ShippingAddressId = address.Id,
            Items = new List<OrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductNameFr = "Test", ProductNameEn = "Test", Quantity = 1, PriceHT = 100m, VatRate = VatRate.Standard }
            },
            StatusHistory = new List<OrderStatusChange>(),
            Invoices = []
        };
    }
}
