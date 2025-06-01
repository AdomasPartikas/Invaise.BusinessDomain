using AutoMapper;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.GaiaAPIClient;

namespace Invaise.BusinessDomain.API.Profiles;

/// <summary>
/// AutoMapper profile for mapping between PredictionResponse and Heat entities
/// </summary>
public class HeatProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the HeatProfile class and configures mappings
    /// </summary>
    public HeatProfile()
    {
        CreateMap<PredictionResponse, Heat>()
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Symbol))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => (int)(src.Combined_heat.Heat_score * 100))) // Convert to percentage
            .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => (int)(src.Combined_heat.Confidence * 100))) // Convert to percentage
            .ForMember(dest => dest.HeatScore, opt => opt.MapFrom(src => src.Combined_heat.Heat_score))
            .ForMember(dest => dest.Explanation, opt => opt.MapFrom(src => src.Combined_heat.Explanation))
            .ForMember(dest => dest.Direction, opt => opt.MapFrom(src => src.Combined_heat.Direction))
            .ForMember(dest => dest.ApolloContribution, opt => opt.MapFrom(src => src.Combined_heat.Apollo_contribution))
            .ForMember(dest => dest.IgnisContribution, opt => opt.MapFrom(src => src.Combined_heat.Ignis_contribution))
            .ForMember(dest => dest.PredictionId, opt => opt.MapFrom(src => (long)src.Combined_heat.Prediction_id)); // Assuming ID is stored in AdditionalProperties
    }
}
