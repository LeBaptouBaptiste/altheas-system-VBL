using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Controllers;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Payments;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;
// Use a namespace alias for Stripe types to avoid collisions with our
// domain enums (PaymentMethod) and entities (Customer, etc.).
using StripeApi = Stripe;

namespace API_Althea_systems.Tests.Controllers;

/// <summary>
/// Guards on POST /api/payments/intents : the AuthZ + state checks are
/// the critical surface here (anything that lets a user pay someone else's
/// order, double-charge, or trigger a Stripe call on a cancelled order
/// is a bug). The Stripe SDK call itself is mocked through IStripeService.
/// </summary>
public class PaymentIntentControllerTests
{
    private readonly Mock<IStripeService> _stripe = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IUserRepository> _users = new();

    private static PaymentIntentController BuildSut(
        IStripeService stripe,
        IOrderRepository orders,
        IUserRepository users,
        Guid? authedUserId,
        string role = "Customer")
    {
        var controller = new PaymentIntentController(stripe, orders, users);
        var http = new DefaultHttpContext();
        if (authedUserId.HasValue)
            http.Items["UserId"] = authedUserId.Value.ToString();
        http.Items["UserRole"] = role;
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private static Order MakeOrder(
        Guid ownerId,
        PaymentMethod paymentMethod = PaymentMethod.Card,
        PaymentStatus paymentStatus = PaymentStatus.Pending,
        OrderStatus status = OrderStatus.Pending)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Date = DateTime.UtcNow,
            Status = status,
            PaymentStatus = paymentStatus,
            PaymentMethod = paymentMethod,
            BillingAddressId = Guid.NewGuid(),
            ShippingAddressId = Guid.NewGuid(),
            ShippingMethod = ShippingMethod.Standard,
            ShippingCost = 15m,
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductNameFr = "Test",
                    ProductNameEn = "Test",
                    Quantity = 1,
                    PriceHT = 100m,
                    VatRate = VatRate.Standard,
                },
            },
        };

    private static User MakeUser(Guid id) => new()
    {
        Id = id,
        Name = "Test User",
        Email = "test@test.com",
        PasswordHash = "irrelevant",
        Role = UserRole.Customer,
        Status = UserStatus.Active,
    };

    [Fact]
    public async Task CreateIntent_UnknownOrder_ThrowsNotFound()
    {
        var caller = Guid.NewGuid();
        _orders.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);
        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, caller);

        var act = () => sut.CreateIntent(new CreatePaymentIntentRequest(Guid.NewGuid(), false), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateIntent_OtherUsersOrder_ThrowsForbidden()
    {
        var caller = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var order = MakeOrder(owner);
        _orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, caller);

        var act = () => sut.CreateIntent(new CreatePaymentIntentRequest(order.Id, false), default);

        await act.Should().ThrowAsync<ForbiddenException>();
        // Stripe must NOT be touched if AuthZ fails.
        _stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateIntent_AdminOnOtherUsersOrder_StillForbidden()
    {
        // Admins are intentionally not allowed via this endpoint — paying
        // for someone else's order should not be possible at all.
        var admin = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var order = MakeOrder(owner);
        _orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, admin, role: "Admin");

        var act = () => sut.CreateIntent(new CreatePaymentIntentRequest(order.Id, false), default);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Theory]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.AdminMandate)]
    public async Task CreateIntent_NonCardPaymentMethod_ThrowsValidation(PaymentMethod method)
    {
        var owner = Guid.NewGuid();
        var order = MakeOrder(owner, paymentMethod: method);
        _orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, owner);

        var act = () => sut.CreateIntent(new CreatePaymentIntentRequest(order.Id, false), default);

        await act.Should().ThrowAsync<AppValidationException>();
        _stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateIntent_AlreadyPaid_ThrowsValidation()
    {
        var owner = Guid.NewGuid();
        var order = MakeOrder(owner, paymentStatus: PaymentStatus.Validated);
        _orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, owner);

        var act = () => sut.CreateIntent(new CreatePaymentIntentRequest(order.Id, false), default);

        await act.Should().ThrowAsync<AppValidationException>();
        _stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateIntent_CancelledOrder_ThrowsValidation()
    {
        var owner = Guid.NewGuid();
        var order = MakeOrder(owner, status: OrderStatus.Cancelled);
        _orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, owner);

        var act = () => sut.CreateIntent(new CreatePaymentIntentRequest(order.Id, false), default);

        await act.Should().ThrowAsync<AppValidationException>();
        _stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateIntent_HappyPath_PersistsIntentIdAndReturnsClientSecret()
    {
        var owner = Guid.NewGuid();
        var order = MakeOrder(owner);
        var user = MakeUser(owner);
        var intent = new StripeApi.PaymentIntent
        {
            Id = "pi_test_123",
            ClientSecret = "pi_test_123_secret_xyz",
            Status = "requires_payment_method",
            Amount = 13800,
            Currency = "eur",
        };

        _orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _users.Setup(r => r.GetByIdAsync(owner)).ReturnsAsync(user);
        _stripe
            .Setup(s => s.CreatePaymentIntentAsync(order, user, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(intent);

        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, owner);

        var result = await sut.CreateIntent(new CreatePaymentIntentRequest(order.Id, false), default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<CreatePaymentIntentResponse>().Subject;
        payload.ClientSecret.Should().Be(intent.ClientSecret);
        payload.PaymentIntentId.Should().Be("pi_test_123");
        payload.Amount.Should().Be(13800);
        payload.Currency.Should().Be("eur");

        // The order must be mutated with the intent id + raw status BEFORE
        // returning, so a webhook arriving in the same millisecond can match.
        order.StripePaymentIntentId.Should().Be("pi_test_123");
        order.StripePaymentStatus.Should().Be("requires_payment_method");
        _orders.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task CreateIntent_SaveCardTrue_PassesFlagToStripeService()
    {
        var owner = Guid.NewGuid();
        var order = MakeOrder(owner);
        var user = MakeUser(owner);
        var intent = new StripeApi.PaymentIntent { Id = "pi_x", ClientSecret = "sec", Status = "x", Currency = "eur" };

        _orders.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _users.Setup(r => r.GetByIdAsync(owner)).ReturnsAsync(user);
        _stripe
            .Setup(s => s.CreatePaymentIntentAsync(order, user, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(intent);

        var sut = BuildSut(_stripe.Object, _orders.Object, _users.Object, owner);

        await sut.CreateIntent(new CreatePaymentIntentRequest(order.Id, SaveCard: true), default);

        _stripe.Verify(s =>
            s.CreatePaymentIntentAsync(order, user, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
