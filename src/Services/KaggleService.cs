using Invaise.BusinessDomain.API.Interfaces;
using System.Diagnostics;
using Invaise.BusinessDomain.API.Constants;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for handling kaggle data operations.
/// </summary>
public class KaggleService : IKaggleService
{
    /// <summary>
    /// Downloads a dataset from Kaggle.
    /// </summary>
    /// <param name="datasetUrl">The URL of the dataset to download.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DownloadDatasetAsync(string datasetUrl)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var dataPath = Path.GetFullPath(Path.Combine(baseDirectory, GlobalConstants.DataFolder));

        Directory.CreateDirectory(dataPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "kaggle",
            Arguments = $"datasets download -d {datasetUrl} --unzip --path {dataPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Error downloading dataset:\nSTDOUT:\n{output}\nSTDERR:\n{error}");

        Console.WriteLine($"Dataset downloaded successfully: {output}");
    }
}