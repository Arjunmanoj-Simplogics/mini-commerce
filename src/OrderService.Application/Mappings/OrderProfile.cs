using AutoMapper;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mappings;

/// <summary>
/// AutoMapper profile for order mappings.
/// </summary>
public class OrderProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderProfile"/> class.
    /// </summary>
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
