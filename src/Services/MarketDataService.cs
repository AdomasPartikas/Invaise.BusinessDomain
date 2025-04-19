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
public class MarketDataService(IFinnhubClient finnhubClient, IMapper mapper, InvaiseDbContext context, IKaggleService kaggleService, IDataService dataService, IDatabaseService dbService) : IMarketDataService
{
    public async Task FetchAndImportMarketDataAsync()
    {
        await kaggleService.DownloadDatasetAsync(GlobalConstants.KaggleSmpDataset);

        await dataService.SMPDatasetCleanupAsync();

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

    private async Task<CompanyProfile2?> GetCompanyProfile(string symbol, int retries = 5, int delay = 1)
    {
        var company = new CompanyProfile2();

        int retryCount = 0;

        do
        {
            await Task.Delay(delay * 5);

            try
            {
                company = await finnhubClient.CompanyProfile2Async(symbol, null, null);
                return company;
            }
            catch (FinnhubAPIClientException ex)
            {
                if (ex.StatusCode == 429)
                {
                    retryCount++;
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching company data for {symbol}: {ex.Message}");
                return null;
            }

        } while (retryCount < retries);

        return company;
    }

    public async Task ImportCompanyDataAsync()
    {
        var symbols = (await dbService.GetAllUniqueMarketDataSymbolsAsync())
            .OrderBy(symbol => symbol)
            .ToList();

        var retries = GlobalConstants.Retries;
        var delay = GlobalConstants.RetryDelaySeconds;

        foreach (var symbol in symbols)
        {
            var existingCompany = await context.Companies
                .FirstOrDefaultAsync(c => c.Symbol == symbol);

            if (existingCompany != null && existingCompany.Name != null && existingCompany.Description != null && 
                existingCompany.Country != null && existingCompany.Industry != null && existingCompany.Symbol != null)
            {
                continue;
            }
            else if (existingCompany == null)
            {
                existingCompany = new Entities.Company { Symbol = symbol };
                context.Companies.Add(existingCompany);
            }

            var company = await GetCompanyProfile(symbol, retries, delay);

            if (company != null)
            {
                mapper.Map(company, existingCompany);
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"Inserted {context.ChangeTracker.Entries<Entities.Company>().Count()} new company records.");
    }
}