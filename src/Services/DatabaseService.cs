using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Context;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;
using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Enums;

namespace Invaise.BusinessDomain.API.Services;

/// <summary>
/// Provides methods for interacting with the database to retrieve market data.
/// </summary>
/// <remarks>
/// This service is responsible for querying the database for market data and symbols.
/// It includes methods to retrieve unique market data symbols and filter market data
/// based on specific criteria such as symbol, start date, and end date.
/// </remarks>
public class DatabaseService(InvaiseDbContext context) : IDatabaseService
{
    /// <summary>
    /// Retrieves all unique market data symbols from the database.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains 
    /// an enumerable collection of unique market data symbols as strings.
    /// </returns>
    /// <remarks>
    /// This method queries the database for distinct symbols in the MarketData table.
    /// </remarks>
    public async Task<IEnumerable<string>> GetAllUniqueMarketDataSymbolsAsync()
    {
        var symbols = await context.HistoricalMarketData
            .Select(m => m.Symbol)
            .Distinct()
            .ToListAsync();

        return symbols;
    }

    public async Task<IEnumerable<IntradayMarketData>> GetIntradayMarketDataAsync(string symbol, DateTime? start, DateTime? end)
    {
        var query = context.IntradayMarketData.AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(m => m.Symbol == symbol);

        if (start.HasValue)
            query = query.Where(m => m.Timestamp >= start.Value);

        if (end.HasValue)
            query = query.Where(m => m.Timestamp <= end.Value);

        return await query.OrderBy(m => m.Timestamp).ToListAsync();
    }

    public async Task<IntradayMarketData?> GetLatestIntradayMarketDataAsync(string symbol)
    {
        var latestData = await context.IntradayMarketData
            .Where(m => m.Symbol == symbol)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();

        if (latestData == null)
            return null;

        return latestData;
    }

    public async Task<IEnumerable<IntradayMarketData>?> GetLatestIntradayMarketDataAsync(string symbol, int count)
    {
        if (count <= 0)
            count = 1;

        var latestData = await context.IntradayMarketData
            .Where(m => m.Symbol == symbol)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToListAsync();

        if (latestData == null || latestData.Count == 0)
            return null;

        return latestData;
    }

    /// <summary>
    /// Retrieves a collection of market data filtered by the specified criteria.
    /// </summary>
    /// <param name="symbol">The symbol to filter the market data by. If null or empty, no filtering is applied for the symbol.</param>
    /// <param name="start">The start date to filter the market data. If null, no filtering is applied for the start date.</param>
    /// <param name="end">The end date to filter the market data. If null, no filtering is applied for the end date.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an 
    /// <see cref="IEnumerable{MarketData}"/> of market data ordered by date.
    /// </returns>
    public async Task<IEnumerable<HistoricalMarketData>> GetHistoricalMarketDataAsync(string symbol, DateTime? start, DateTime? end)
    {
        var query = context.HistoricalMarketData.AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(m => m.Symbol == symbol);

        if (start.HasValue)
            query = query.Where(m => m.Date >= start.Value);

        if (end.HasValue)
            query = query.Where(m => m.Date <= end.Value);

        return await query.OrderBy(m => m.Date).ToListAsync();
    }

    public async Task<HistoricalMarketData?> GetLatestHistoricalMarketDataAsync(string symbol)
    {
        var latestData = await context.HistoricalMarketData
            .Where(m => m.Symbol == symbol)
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync();

        if (latestData == null)
            return null;

        return latestData;
    }

    public async Task<IEnumerable<HistoricalMarketData>?> GetLatestHistoricalMarketDataAsync(string symbol, int count)
    {
        if (count <= 0)
            count = 1;

        var latestData = await context.HistoricalMarketData
            .Where(m => m.Symbol == symbol)
            .OrderByDescending(m => m.Date)
            .Take(count)
            .ToListAsync();

        if (latestData == null || latestData.Count == 0)
            return null;

        return latestData;
    }
    
    // User Operations
    
    public async Task<User> CreateUserAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await context.Users
            .Include(u => u.PersonalInfo)
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await context.Users
            .Include(u => u.PersonalInfo)
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
    
    public async Task<User> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return user;
    }
    
    public async Task<UserPersonalInfo> UpdateUserPersonalInfoAsync(string userId, UserPersonalInfo personalInfo)
    {
        var existingInfo = await context.UserPersonalInfo.FirstOrDefaultAsync(p => p.UserId == userId);
        
        if (existingInfo == null)
        {
            personalInfo.UserId = userId;
            personalInfo.CreatedAt = DateTime.UtcNow.ToLocalTime();
            personalInfo.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            context.UserPersonalInfo.Add(personalInfo);
        }
        else
        {
            existingInfo.LegalFirstName = personalInfo.LegalFirstName;
            existingInfo.LegalLastName = personalInfo.LegalLastName;
            existingInfo.DateOfBirth = personalInfo.DateOfBirth;
            existingInfo.PhoneNumber = personalInfo.PhoneNumber;
            existingInfo.GovernmentId = personalInfo.GovernmentId;
            existingInfo.Address = personalInfo.Address;
            existingInfo.City = personalInfo.City;
            existingInfo.PostalCode = personalInfo.PostalCode;
            existingInfo.Country = personalInfo.Country;
            existingInfo.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            context.UserPersonalInfo.Update(existingInfo);
            personalInfo = existingInfo;
        }
        
        await context.SaveChangesAsync();
        return personalInfo;
    }
    
    public async Task<UserPreferences> UpdateUserPreferencesAsync(string userId, UserPreferences preferences)
    {
        var existingPreferences = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        
        if (existingPreferences == null)
        {
            preferences.UserId = userId;
            preferences.CreatedAt = DateTime.UtcNow.ToLocalTime();
            preferences.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            context.UserPreferences.Add(preferences);
        }
        else
        {
            existingPreferences.RiskTolerance = preferences.RiskTolerance;
            existingPreferences.InvestmentHorizon = preferences.InvestmentHorizon;
            existingPreferences.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            context.UserPreferences.Update(existingPreferences);
            preferences = existingPreferences;
        }
        
        await context.SaveChangesAsync();
        return preferences;
    }
    
    // Portfolio Operations

    public async Task<IEnumerable<Portfolio>> GetAllPortfoliosAsync()
    {
        return await context.Portfolios
            .Include(p => p.User)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Portfolio>> GetUserPortfoliosAsync(string userId)
    {
        return await context.Portfolios
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }
    
    public async Task<Portfolio?> GetPortfolioByIdAsync(string portfolioId)
    {
        return await context.Portfolios
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId);
    }

    public async Task<Portfolio?> GetPortfolioByIdWithPortfolioStocksAsync(string portfolioId)
    {
        return await context.Portfolios
            .Include(p => p.PortfolioStocks)
            .FirstOrDefaultAsync(p => p.Id == portfolioId);
    }
    
    public async Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio)
    {
        portfolio.CreatedAt = DateTime.UtcNow.ToLocalTime();
        portfolio.LastUpdated = DateTime.UtcNow.ToLocalTime();
        context.Portfolios.Add(portfolio);
        await context.SaveChangesAsync();
        return portfolio;
    }
    
    public async Task<Portfolio> UpdatePortfolioAsync(Portfolio portfolio)
    {
        portfolio.LastUpdated = DateTime.UtcNow.ToLocalTime();
        context.Portfolios.Update(portfolio);
        await context.SaveChangesAsync();
        return portfolio;
    }
    
    public async Task<bool> DeletePortfolioAsync(string portfolioId)
    {
        var portfolio = await context.Portfolios.FindAsync(portfolioId);
        if (portfolio == null)
            return false;
            
        context.Portfolios.Remove(portfolio);
        await context.SaveChangesAsync();
        return true;
    }
    
    // Transaction Operations
    
    public async Task<IEnumerable<Transaction>> GetUserTransactionsAsync(string userId)
    {
        return await context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Transaction>> GetPortfolioTransactionsAsync(string portfolioId)
    {
        return await context.Transactions
            .Where(t => t.PortfolioId == portfolioId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
    
    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        // Calculate transaction value if not set
        if (transaction.TransactionValue == 0)
        {
            transaction.TransactionValue = transaction.Quantity * transaction.PricePerShare;
        }
        
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }
    
    // Portfolio Stock Operations
    
    public async Task<IEnumerable<PortfolioStock>> GetPortfolioStocksAsync(string portfolioId)
    {
        return await context.PortfolioStocks
            .Where(ps => ps.PortfolioId == portfolioId)
            .Include(ps => ps.Portfolio)
            .ToListAsync();
    }
    
    public async Task<PortfolioStock?> GetPortfolioStockByIdAsync(string id)
    {
        return await context.PortfolioStocks
            .Include(ps => ps.Portfolio)
            .FirstOrDefaultAsync(ps => ps.ID == id);
    }
    
    public async Task<PortfolioStock> AddPortfolioStockAsync(PortfolioStock portfolioStock)
    {
        context.PortfolioStocks.Add(portfolioStock);
        await context.SaveChangesAsync();
        return portfolioStock;
    }
    
    public async Task<PortfolioStock> UpdatePortfolioStockAsync(PortfolioStock portfolioStock)
    {
        context.PortfolioStocks.Update(portfolioStock);
        await context.SaveChangesAsync();
        return portfolioStock;
    }
    
    public async Task<bool> DeletePortfolioStockAsync(string id)
    {
        var portfolioStock = await context.PortfolioStocks.FindAsync(id);
        if (portfolioStock == null)
            return false;
            
        context.PortfolioStocks.Remove(portfolioStock);
        await context.SaveChangesAsync();
        return true;
    }
    
    // Company Operations
    
    public async Task<IEnumerable<Company>> GetAllCompaniesAsync()
    {
        return await context.Companies
            .OrderBy(c => c.Symbol)
            .ToListAsync();
    }
    
    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        return await context.Companies
            .FirstOrDefaultAsync(c => c.StockId == id);
    }
    
    public async Task<Company?> GetCompanyBySymbolAsync(string symbol)
    {
        return await context.Companies
            .FirstOrDefaultAsync(c => c.Symbol == symbol);
    }
    
    public async Task<Company> CreateCompanyAsync(Company company)
    {
        company.CreatedAt = DateTime.UtcNow.ToLocalTime();
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        return company;
    }
    
    public async Task<Company> UpdateCompanyAsync(Company company)
    {
        context.Companies.Update(company);
        await context.SaveChangesAsync();
        return company;
    }
    
    public async Task<bool> DeleteCompanyAsync(int id)
    {
        var company = await context.Companies.FindAsync(id);
        if (company == null)
            return false;
            
        context.Companies.Remove(company);
        await context.SaveChangesAsync();
        return true;
    }

    // Service account operations

    public async Task CreateServiceAccountAsync(ServiceAccount serviceAccount)
    {
        context.ServiceAccounts.Add(serviceAccount);
        await context.SaveChangesAsync();
    }

    public async Task<ServiceAccount?> GetServiceAccountAsync(string id)
    {
        return await context.ServiceAccounts
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<ServiceAccount> UpdateServiceAccountAsync(string id, ServiceAccount account)
    {
        var existingAccount = await context.ServiceAccounts.FindAsync(id);

        if (existingAccount == null)
            throw new KeyNotFoundException($"Service account with ID {id} not found.");

        existingAccount.Name = account.Name;
        existingAccount.Permissions = account.Permissions;
        existingAccount.LastAuthenticated = DateTime.UtcNow.ToLocalTime();

        context.ServiceAccounts.Update(existingAccount);
        await context.SaveChangesAsync();
        return existingAccount;
    }

    // Transaction operations

    public async Task<Transaction?> GetTransactionByIdAsync(string id)
    {
        return await context.Transactions
            .Include(t => t.Portfolio)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task CancelTransactionAsync(string id)
    {
        var transaction = await context.Transactions.FindAsync(id);

        if (transaction == null)
            throw new KeyNotFoundException($"Transaction with ID {id} not found.");

        transaction.Status = TransactionStatus.Canceled;

        context.Transactions.Update(transaction);
        await context.SaveChangesAsync();
    }

    // Log operations

    public async Task<IEnumerable<Log>> GetLatestLogsAsync(int count)
    {
        return await context.LogEvents
            .OrderByDescending(l => l.TimeStamp)
            .Take(count)
            .ToListAsync();
    }
}