using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Interfaces;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Invaise.BusinessDomain.API.Services;

public class DataService : IDataService
{
    public async Task SMPDatasetCleanupAsync()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var dataPath = Path.GetFullPath(Path.Combine(baseDirectory, GlobalConstants.DataFolder));

        var inputPath = Path.GetFullPath(Path.Combine(dataPath, GlobalConstants.SmpDatasetRaw));
        var outputPath = Path.GetFullPath(Path.Combine(dataPath, GlobalConstants.SmpDataset));

        var rawRows = await File.ReadAllLinesAsync(inputPath);

        if (rawRows.Length < 4)
            throw new InvalidOperationException("CSV is too short or not in expected format.");

        var tickerLine = rawRows[1].Split(','); // row index 1
        var tickerList = tickerLine.Skip(1).Take(GlobalConstants.TickerCount).ToList(); 

        var finalRows = new List<FlatRow>();

        for (int rowIndex = 3; rowIndex < rawRows.Length; rowIndex++)
        {
            var row = rawRows[rowIndex].Split(',');
            if (row.Length < 1 + GlobalConstants.TickerCount * GlobalConstants.PriceBlocks)
                continue;

            if (!DateTime.TryParse(row[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateParsed))
                continue;

            for (int t = 0; t < GlobalConstants.TickerCount; t++)
            {
                int baseIndex = 1 + t;

                var record = new FlatRow
                {
                    Symbol = tickerList[t],
                    Date = dateParsed.ToString("yyyy/MM/dd HH:mm:ss"),
                    CreatedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    Close = TryParseDecimal(row[baseIndex]),
                    High = TryParseDecimal(row[baseIndex + GlobalConstants.TickerCount * 1]),
                    Low = TryParseDecimal(row[baseIndex + GlobalConstants.TickerCount * 2]),
                    Open = TryParseDecimal(row[baseIndex + GlobalConstants.TickerCount * 3]),
                    Volume = TryParseDecimal(row[baseIndex + GlobalConstants.TickerCount * 4]),
                };

                if (record.Close == null && record.High == null && record.Low == null &&
                    record.Open == null && record.Volume == null)
                    continue;

                finalRows.Add(record);
            }
        }

        using var writer = new StreamWriter(outputPath);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        await csv.WriteRecordsAsync(finalRows);

        Console.WriteLine($"Transformed {finalRows.Count} rows. Written to {outputPath}");
    }

    private decimal? TryParseDecimal(string? value)
    {
        return decimal.TryParse(value, out var result) ? result : null;
    }

    private class FlatRow
    {
        public string Symbol { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public decimal? Open { get; set; } = null;
        public decimal? High { get; set; } = null;
        public decimal? Low { get; set; } = null;
        public decimal? Close { get; set; } = null;
        public decimal? Volume { get; set; } = null;
        public string CreatedAt { get; set; } = string.Empty;
    }
}