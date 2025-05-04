using AutoMapper;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.GaiaAPIClient;

namespace Invaise.BusinessDomain.API.Profiles;

/// <summary>
/// AutoMapper profile for mapping OptimizationResponse to PortfolioOptimizationResult
/// </summary>
public class PortfolioOptimizationProfile : Profile
{
    public PortfolioOptimizationProfile()
    {
        CreateMap<OptimizationResponse, PortfolioOptimizationResult>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Explanation, opt => opt.MapFrom(src => src.Explanation))
            .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Confidence))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => DateTime.Parse(src.Timestamp)))
            .ForMember(dest => dest.Metrics, opt => opt.MapFrom(src => new PortfolioMetrics
            {
                SharpeRatio = src.SharpeRatio,
                MeanReturn = src.MeanReturn,
                Variance = src.Variance,
                ExpectedReturn = src.ExpectedReturn
            }))
            .ForMember(dest => dest.Recommendations, opt => opt.MapFrom(src => src.Recommendations))
            .ForMember(dest => dest.Successful, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore());

        CreateMap<RecommendationData, PortfolioRecommendation>()
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Symbol))
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action))
            .ForMember(dest => dest.CurrentQuantity, opt => opt.MapFrom(src => (decimal)src.CurrentQuantity))
            .ForMember(dest => dest.TargetQuantity, opt => opt.MapFrom(src => (decimal)src.TargetQuantity))
            .ForMember(dest => dest.CurrentWeight, opt => opt.MapFrom(src => src.CurrentWeight))
            .ForMember(dest => dest.TargetWeight, opt => opt.MapFrom(src => src.TargetWeight))
            .ForMember(dest => dest.Explanation, opt => opt.MapFrom(src => src.Explanation));

                // Mapping from PortfolioOptimizationResult to PortfolioOptimization
        CreateMap<PortfolioOptimizationResult, PortfolioOptimization>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.PortfolioId, opt => opt.Ignore()) // PortfolioId should be set explicitly
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.Explanation, opt => opt.MapFrom(src => src.Explanation))
            .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Confidence))
            .ForMember(dest => dest.ModelVersion, opt => opt.Ignore()) // ModelVersion should be set explicitly
            .ForMember(dest => dest.IsApplied, opt => opt.MapFrom(src => false)) // Default to false
            .ForMember(dest => dest.SharpeRatio, opt => opt.MapFrom(src => src.Metrics.SharpeRatio))
            .ForMember(dest => dest.MeanReturn, opt => opt.MapFrom(src => src.Metrics.MeanReturn))
            .ForMember(dest => dest.Variance, opt => opt.MapFrom(src => src.Metrics.Variance))
            .ForMember(dest => dest.ExpectedReturn, opt => opt.MapFrom(src => src.Metrics.ExpectedReturn))
            .ForMember(dest => dest.ProjectedSharpeRatio, opt => opt.MapFrom(src => src.Metrics.ProjectedSharpeRatio))
            .ForMember(dest => dest.ProjectedMeanReturn, opt => opt.MapFrom(src => src.Metrics.ProjectedMeanReturn))
            .ForMember(dest => dest.ProjectedVariance, opt => opt.MapFrom(src => src.Metrics.ProjectedVariance))
            .ForMember(dest => dest.ProjectedExpectedReturn, opt => opt.MapFrom(src => src.Metrics.ProjectedExpectedReturn))
            .ForMember(dest => dest.Recommendations, opt => opt.MapFrom(src => src.Recommendations))
            .ForMember(dest => dest.AppliedDate, opt => opt.Ignore()) // AppliedDate should be set explicitly
            .ForMember(dest => dest.RiskTolerance, opt => opt.Ignore()) // RiskTolerance should be set explicitly
            .ForMember(dest => dest.Portfolio, opt => opt.Ignore()); // Navigation property should be set explicitly

        // Mapping from PortfolioRecommendation to PortfolioOptimizationRecommendation
        CreateMap<PortfolioRecommendation, PortfolioOptimizationRecommendation>()
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Symbol))
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action))
            .ForMember(dest => dest.CurrentQuantity, opt => opt.MapFrom(src => src.CurrentQuantity))
            .ForMember(dest => dest.TargetQuantity, opt => opt.MapFrom(src => src.TargetQuantity))
            .ForMember(dest => dest.CurrentWeight, opt => opt.MapFrom(src => src.CurrentWeight))
            .ForMember(dest => dest.TargetWeight, opt => opt.MapFrom(src => src.TargetWeight))
            .ForMember(dest => dest.Explanation, opt => opt.MapFrom(src => src.Explanation))
            .ForMember(dest => dest.Optimization, opt => opt.Ignore()); // Navigation property should be set explicitly


        CreateMap<PortfolioOptimization, PortfolioOptimizationResult>()
            .ForMember(dest => dest.OptimizationId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Explanation, opt => opt.MapFrom(src => src.Explanation))
            .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.Confidence))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.Metrics, opt => opt.MapFrom(src => new PortfolioMetrics
            {
                SharpeRatio = src.SharpeRatio,
                MeanReturn = src.MeanReturn,
                Variance = src.Variance,
                ExpectedReturn = src.ExpectedReturn,
                ProjectedSharpeRatio = src.ProjectedSharpeRatio,
                ProjectedMeanReturn = src.ProjectedMeanReturn,
                ProjectedVariance = src.ProjectedVariance,
                ProjectedExpectedReturn = src.ProjectedExpectedReturn
            }))
            .ForMember(dest => dest.Recommendations, opt => opt.MapFrom(src => src.Recommendations))
            .ForMember(dest => dest.Successful, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore());

        CreateMap<PortfolioOptimizationRecommendation, PortfolioRecommendation>()
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Symbol))
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action))
            .ForMember(dest => dest.CurrentQuantity, opt => opt.MapFrom(src => src.CurrentQuantity))
            .ForMember(dest => dest.TargetQuantity, opt => opt.MapFrom(src => src.TargetQuantity))
            .ForMember(dest => dest.CurrentWeight, opt => opt.MapFrom(src => src.CurrentWeight))
            .ForMember(dest => dest.TargetWeight, opt => opt.MapFrom(src => src.TargetWeight))
            .ForMember(dest => dest.Explanation, opt => opt.MapFrom(src => src.Explanation));
    }
}