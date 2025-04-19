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

        // CreateMap<Quote, MarketDataDto>()
        //     .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Symbol))
        //     .ForMember(dest => dest.Open, opt => opt.MapFrom(src => src.Open))
        //     .ForMember(dest => dest.Close, opt => opt.MapFrom(src => src.Close))
        //     .ForMember(dest => dest.High, opt => opt.MapFrom(src => src.High))
        //     .ForMember(dest => dest.Low, opt => opt.MapFrom(src => src.Low))
        //     .ForMember(dest => dest.Volume, opt => opt.MapFrom(src => src.Volume))
        //     .ForMember(dest => dest.Time, opt => opt.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds(src.Time).UtcDateTime));
    }
}