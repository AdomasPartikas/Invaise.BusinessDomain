using AutoMapper;
using BusinessDomain.FinnhubAPIClient;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Profiles;

/// <summary>
/// Profile for mapping data into and from the MarketData entity.
/// </summary>
public class MarketDataProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketDataProfile"/> class.
    /// </summary>
    public MarketDataProfile()
    {
        CreateMap<MarketDataDto, MarketData>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}