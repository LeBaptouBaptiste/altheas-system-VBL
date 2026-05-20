using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Controllers;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Tests.Controllers;

/// <summary>
/// Verifies the ownership guard on /api/orders/{id} and that the listing
/// endpoint silently rewrites the userId filter for non-admin callers.
/// </summary>
public class OrderControllerTests
{
    private readonly Mock<IOrderService> _orderService = new();

    private static OrderController BuildSut(IOrderService svc, Guid? authedUserId, string role = "Customer")
    {
        var controller = new OrderController(svc);
        var http = new DefaultHttpContext();
        if (authedUserId.HasValue)
            http.Items["UserId"] = authedUserId.Value.ToString();
        http.Items["UserRole"] = role;
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private static OrderDto MakeDto(Guid orderId, Guid ownerId)
    {
        var addr = new AddressDto(Guid.NewGuid(), "x", "x", "x", null, "x", null, "x", "x", "x", null);
        return new OrderDto(
            orderId, ownerId, "owner",
            DateTime.UtcNow,
            OrderStatus.Pending, PaymentStatus.Pending, PaymentMethod.Card,
            addr, addr,
            ShippingMethod.Standard, 15m,
            100m, 20m, 135m,
            DateTime.UtcNow, DateTime.UtcNow,
            Items: [],
            StatusHistory: [],
            // LatestInvoiceId (phase 3) + Invoices (phase 6) — both empty
            // for a freshly-created Pending order.
            LatestInvoiceId: null,
            Invoices: []);
    }

    [Fact]
    public async Task GetById_OtherUsersOrder_ThrowsForbidden()
    {
        var caller = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _orderService.Setup(s => s.GetByIdAsync(orderId)).ReturnsAsync(MakeDto(orderId, owner));
        var sut = BuildSut(_orderService.Object, caller, role: "Customer");

        var act = () => sut.GetById(orderId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetById_OwnOrder_Succeeds()
    {
        var owner = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _orderService.Setup(s => s.GetByIdAsync(orderId)).ReturnsAsync(MakeDto(orderId, owner));
        var sut = BuildSut(_orderService.Object, owner, role: "Customer");

        var result = await sut.GetById(orderId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_AdminOnAnyOrder_Succeeds()
    {
        var admin = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        _orderService.Setup(s => s.GetByIdAsync(orderId)).ReturnsAsync(MakeDto(orderId, owner));
        var sut = BuildSut(_orderService.Object, admin, role: "Admin");

        var result = await sut.GetById(orderId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_NonAdmin_OverridesUserIdToCaller()
    {
        var caller = Guid.NewGuid();
        var sut = BuildSut(_orderService.Object, caller, role: "Customer");
        _orderService
            .Setup(s => s.GetAllAsync(1, 12, caller))
            .ReturnsAsync(new API_Althea_systems.Models.Shared.PaginatedResponse<OrderDto>([], 1, 12, 0, 0));

        // Caller tries to query someone else's orders — controller must silently
        // rewrite the filter to the caller's id.
        var attackerTarget = Guid.NewGuid();
        await sut.GetAll(userId: attackerTarget);

        _orderService.Verify(s => s.GetAllAsync(1, 12, caller), Times.Once);
        _orderService.Verify(s => s.GetAllAsync(1, 12, attackerTarget), Times.Never);
    }

    [Fact]
    public async Task GetAll_Admin_KeepsRequestedUserIdFilter()
    {
        var admin = Guid.NewGuid();
        var target = Guid.NewGuid();
        var sut = BuildSut(_orderService.Object, admin, role: "Admin");
        _orderService
            .Setup(s => s.GetAllAsync(1, 12, target))
            .ReturnsAsync(new API_Althea_systems.Models.Shared.PaginatedResponse<OrderDto>([], 1, 12, 0, 0));

        await sut.GetAll(userId: target);

        _orderService.Verify(s => s.GetAllAsync(1, 12, target), Times.Once);
    }
}
