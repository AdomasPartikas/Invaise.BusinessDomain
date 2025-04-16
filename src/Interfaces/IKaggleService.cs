namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Provides methods for retrieving kaggle datasets.
/// </summary>
public interface IKaggleService
{
    /// <summary>
    /// Downloads a dataset from Kaggle.
    /// </summary>
    /// <param name="datasetUrl">The URL of the dataset to download.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DownloadDatasetAsync(string datasetUrl);
}