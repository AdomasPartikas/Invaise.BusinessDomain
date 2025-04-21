using System.ComponentModel;
using AutoMapper;
using Invaise.BusinessDomain.API.FinnhubAPIClient;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Utils;

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
        CreateMap<MarketDataDto, HistoricalMarketData>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<Quote, IntradayMarketData>()
            .ForMember(dest => dest.Open, opt => opt.MapFrom(src => src.O))
            .ForMember(dest => dest.Current, opt => opt.MapFrom(src => src.C))
            .ForMember(dest => dest.High, opt => opt.MapFrom(src => src.H))
            .ForMember(dest => dest.Low, opt => opt.MapFrom(src => src.L))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => Utils.DateTimeConverter.UnixTimestampToDateTime(src.T!.Value)));
    }
}