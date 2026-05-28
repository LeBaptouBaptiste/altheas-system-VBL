using FluentValidation;
using API_Althea_systems.Models.Order;

namespace API_Althea_systems.Validators;

public class OrderCreateRequestValidator : AbstractValidator<OrderCreateRequest>
{
    public OrderCreateRequestValidator()
    {
        RuleFor(x => x.BillingAddressId)
            .NotEmpty().WithMessage("Billing address is required.");

        RuleFor(x => x.ShippingAddressId)
            .NotEmpty().WithMessage("Shipping address is required.");

        RuleFor(x => x.ShippingMethod)
            .IsInEnum().WithMessage("Invalid shipping method.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items).SetValidator(new OrderItemCreateRequestValidator());
    }
}

public class OrderItemCreateRequestValidator : AbstractValidator<OrderItemCreateRequest>
{
    public OrderItemCreateRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product id is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(1000).WithMessage("Quantity must not exceed 1000.");
    }
}
