using AutoMapper;
using FluentValidation;
using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Constants;
using InventoryService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryService.Application;

public class InventoryProfile : Profile
{
    public InventoryProfile()
    {
        CreateMap<InventoryItem, InventoryItemDto>();
    }
}

public class CreateInventoryItemValidator : AbstractValidator<CreateInventoryItemDto>
{
    public CreateInventoryItemValidator()
    {
        RuleFor(x => x.ProductSku).NotEmpty().MaximumLength(InventoryConstants.MaxSkuLength);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(InventoryConstants.MaxProductNameLength);
        RuleFor(x => x.QuantityAvailable).GreaterThanOrEqualTo(0);
    }
}

public class UpdateInventoryItemValidator : AbstractValidator<UpdateInventoryItemDto>
{
    public UpdateInventoryItemValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(InventoryConstants.MaxProductNameLength);
        RuleFor(x => x.QuantityAvailable).GreaterThanOrEqualTo(0);
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMapper>(_ =>
            new MapperConfiguration(cfg => cfg.AddProfile<InventoryProfile>()).CreateMapper());
        services.AddScoped<IInventoryService, Services.InventoryService>();
        services.AddValidatorsFromAssemblyContaining<CreateInventoryItemValidator>();
        return services;
    }
}
