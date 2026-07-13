using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappings;
using OrderService.Application.Services;
using OrderService.Application.Validators;

namespace OrderService.Application;

/// <summary>
/// Extension methods for registering application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMapper>(_ =>
        {
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>());
            return configuration.CreateMapper();
        });
        services.AddScoped<IOrderService, Services.OrderService>();
        services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

        return services;
    }
}
