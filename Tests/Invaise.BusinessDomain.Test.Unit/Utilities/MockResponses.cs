using System;
using System.Collections.Generic;

namespace Invaise.BusinessDomain.Test.Unit.Utilities;

/// <summary>
/// Mock response classes for testing API clients
/// </summary>
public static class MockResponses
{
    // Apollo API Client mocks
    public static class ApolloAPI
    {
        public class HealthResponse
        {
            public string Status { get; set; } = string.Empty;
        }

        public class InfoResponse
        {
            public string? Model_version { get; set; }
            public string? Description { get; set; }
            public DateTime? Last_trained { get; set; }
        }

        public class PredictResponse
        {
            public string Symbol { get; set; } = string.Empty;
            public double Heat_score { get; set; }
            public double Confidence { get; set; }
            public string? Direction { get; set; }
            public string? Explanation { get; set; }
            public double Predicted_next_close { get; set; }
        }

        public class TrainingStartResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public class TrainingStatusResponse
        {
            public bool Is_training { get; set; }
            public string? Started_at { get; set; }
            public int Progress { get; set; }
        }
    }

    // Ignis API Client mocks
    public static class IgnisAPI
    {
        public class HealthResponse
        {
            public string Status { get; set; } = string.Empty;
        }

        public class InfoResponse
        {
            public string? Model_version { get; set; }
            public string? Description { get; set; }
            public DateTime? Last_trained { get; set; }
        }

        public class PredictionResponse
        {
            public string Symbol { get; set; } = string.Empty;
            public double Heat_score { get; set; }
            public double Confidence { get; set; }
            public string? Direction { get; set; }
            public string? Explanation { get; set; }
            public double Pred_close { get; set; }
        }

        public class TrainingResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public class StatusResponse
        {
            public string Status { get; set; } = string.Empty;
        }
    }

    // Gaia API Client mocks
    public static class GaiaAPI
    {
        public class HealthResponse
        {
            public string Status { get; set; } = string.Empty;
        }

        public class HeatData
        {
            public double Heat_score { get; set; }
            public double Confidence { get; set; }
            public string Direction { get; set; } = string.Empty;
            public string Timestamp { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string Explanation { get; set; } = string.Empty;
            public double Apollo_contribution { get; set; }
            public double Ignis_contribution { get; set; }
            public long Prediction_id { get; set; }
            public string Model_version { get; set; } = string.Empty;
            public string Prediction_target { get; set; } = string.Empty;
            public double Current_price { get; set; }
            public double Predicted_price { get; set; }
        }

        public class GaiaPredictionResponse
        {
            public string Symbol { get; set; } = string.Empty;
            public HeatData Combined_heat { get; set; } = new HeatData();
        }

        public class PredictionRequest
        {
            public string Symbol { get; set; } = string.Empty;
            public string Portfolio_id { get; set; } = string.Empty;
        }

        public class WeightAdjustRequest
        {
            public double Apollo_weight { get; set; }
            public double Ignis_weight { get; set; }
        }

        public class OptimizationRequest
        {
            public string Portfolio_id { get; set; } = string.Empty;
        }

        public class RecommendationData
        {
            public string Symbol { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public int CurrentQuantity { get; set; }
            public int TargetQuantity { get; set; }
            public double CurrentWeight { get; set; }
            public double TargetWeight { get; set; }
            public string Explanation { get; set; } = string.Empty;
        }

        public class OptimizationResponse
        {
            public string Id { get; set; } = string.Empty;
            public string PortfolioId { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string Timestamp { get; set; } = string.Empty;
            public string Explanation { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public double RiskTolerance { get; set; }
            public bool IsApplied { get; set; }
            public string ModelVersion { get; set; } = string.Empty;
            public double SharpeRatio { get; set; }
            public double MeanReturn { get; set; }
            public double Variance { get; set; }
            public double ExpectedReturn { get; set; }
            public double? ProjectedSharpeRatio { get; set; }
            public double? ProjectedMeanReturn { get; set; }
            public double? ProjectedVariance { get; set; }
            public double? ProjectedExpectedReturn { get; set; }
            public List<RecommendationData> Recommendations { get; set; } = new List<RecommendationData>();
            public List<string> SymbolsProcessed { get; set; } = new List<string>();
            public string PortfolioStrategy { get; set; } = string.Empty;
        }
    }
} 