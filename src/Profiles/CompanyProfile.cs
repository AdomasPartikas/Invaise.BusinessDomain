using AutoMapper;
using Invaise.BusinessDomain.API.FinnhubAPIClient;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Profiles;

/// <summary>
/// Profile for mapping data into and from the MarketData entity.
/// </summary>
public class CompanyProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketDataProfile"/> class.
    /// </summary>
    public CompanyProfile()
    {
        CreateMap<CompanyDto, Entities.Company>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow.ToLocalTime()));

        CreateMap<CompanyProfile2, Entities.Company>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow.ToLocalTime()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Industry, opt => opt.MapFrom(src => src.FinnhubIndustry))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Exchange))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country));
    }
}