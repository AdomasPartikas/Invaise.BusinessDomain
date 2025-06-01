using System.Threading;
using System.Threading.Tasks;
using static Invaise.BusinessDomain.Test.Unit.Utilities.MockResponses.IgnisAPI;

namespace Invaise.BusinessDomain.API.Interfaces
{
    // Mock interfaces for Ignis API clients to use in unit tests
    
    public interface IHealthIgnisClient
    {
        Task<HealthResponse> GetAsync();
        Task<HealthResponse> GetAsync(CancellationToken cancellationToken);
    }

    public interface IPredictIgnisClient
    {
        Task<PredictionResponse> GetAsync(string symbol, string? date, CancellationToken cancellationToken);
        Task<PredictionResponse> GetAsync(string symbol, string? date);
    }

    public interface IInfoIgnisClient
    {
        Task<InfoResponse> GetAsync();
        Task<InfoResponse> GetAsync(CancellationToken cancellationToken);
    }

    public interface ITrainIgnisClient
    {
        Task<TrainingResponse> PostAsync();
        Task<TrainingResponse> PostAsync(CancellationToken cancellationToken);
    }

    public interface IStatusIgnisClient
    {
        Task<StatusResponse> GetAsync();
        Task<StatusResponse> GetAsync(CancellationToken cancellationToken);
    }
} 