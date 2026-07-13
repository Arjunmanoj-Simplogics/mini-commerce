using FluentValidation;
using OrderService.Application.DTOs;
using OrderService.Domain.Constants;
using OrderService.Domain.Enums;

namespace OrderService.Application.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(OrderConstants.MaxCustomerNameLength);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(OrderConstants.MaxEmailLength);
        RuleFor(x => x.ProductSku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.TotalAmount).GreaterThan(0);
    }
}

public class UpdateOrderValidator : AbstractValidator<UpdateOrderDto>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(OrderConstants.MaxCustomerNameLength);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(OrderConstants.MaxEmailLength);
        RuleFor(x => x.ProductSku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => Enum.TryParse<OrderStatus>(status, ignoreCase: true, out _))
            .WithMessage("Status must be a valid order status value.");
    }
}
