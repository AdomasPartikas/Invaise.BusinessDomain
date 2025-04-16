using CsvHelper;
using CsvHelper.Configuration;
using AutoMapper;
using BusinessDomain.FinnhubAPIClient;
using System.Globalization;
using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Models;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Service for handling market data operations.
/// </summary>
public class MarketDataService(IFinnhubClient finnhubClient, IMapper mapper, InvaiseDbContext context, IKaggleService kaggleService, IDataService dataService) : IMarketDataService
{
    public async Task FetchAndImportMarketDataAsync()
    {
        //await kaggleService.DownloadDatasetAsync(GlobalConstants.KaggleSmpDataset);

        //await dataService.SMPDatasetCleanupAsync();

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var dataPath = Path.GetFullPath(Path.Combine(baseDirectory, GlobalConstants.DataFolder));

        var cleanedFilePath = Path.GetFullPath(Path.Combine(dataPath, GlobalConstants.SmpDataset));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var reader = new StreamReader(cleanedFilePath);
        using var csv = new CsvReader(reader, config);
        var newData = csv.GetRecords<MarketDataDto>().ToList();

        var grouped = newData.GroupBy(x => x.Symbol);

        var newEntities = new List<MarketData>();

        foreach (var group in grouped)
        {
            var s = group.Key;

            // Get the latest date from DB for this symbol
            var latestDate = await context.MarketData
                .Where(m => m.Symbol == s)
                .MaxAsync(m => (DateTime?)m.Date) ?? DateTime.MinValue;

            // Filter only newer entries
            var freshRecords = group.Where(g => g.Date > latestDate);

            foreach (var record in freshRecords)
            {
                if(record.Symbol == "AAPL")
                    Console.WriteLine($"Processing {record.Symbol} - {record.Date}");
                
                var entity = mapper.Map<MarketData>(record);
                newEntities.Add(entity);
            }
        }

        if (newEntities.Count > 0)
        {
            await context.MarketData.AddRangeAsync(newEntities);
            await context.SaveChangesAsync();
            Console.WriteLine($"Inserted {newEntities.Count} new market data records.");
        }
        else
            Console.WriteLine("No new market data to insert.");

    }

    public async Task<IEnumerable<MarketDataDto>> GetStockQuote(string symbol)
    {
        throw new NotImplementedException();
        
        var stockQuote = await finnhubClient.QuoteAsync(symbol);

        var marketData = mapper.Map<MarketDataDto>(stockQuote);
        // Implementation for retrieving market data from the database
    }
}