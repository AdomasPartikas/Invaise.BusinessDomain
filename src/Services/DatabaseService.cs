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

    /// <summary>
    /// Retrieves intraday market data for a specific symbol within an optional date range
    /// </summary>
    /// <param name="symbol">The stock symbol to retrieve data for</param>
    /// <param name="start">Optional start date for filtering</param>
    /// <param name="end">Optional end date for filtering</param>
    /// <returns>Collection of intraday market data ordered by timestamp</returns>
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

    /// <summary>
    /// Retrieves the latest intraday market data entry for a specific symbol
    /// </summary>
    /// <param name="symbol">The stock symbol to retrieve data for</param>
    /// <returns>The latest intraday market data entry, or null if none found</returns>
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

    /// <summary>
    /// Retrieves the latest intraday market data entries for a specific symbol
    /// </summary>
    /// <param name="symbol">The stock symbol to retrieve data for</param>
    /// <param name="count">The number of latest entries to retrieve</param>
    /// <returns>Collection of the latest intraday market data entries, or null if none found</returns>
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

    /// <summary>
    /// Retrieves the latest historical market data entry for a specific symbol
    /// </summary>
    /// <param name="symbol">The stock symbol to retrieve data for</param>
    /// <returns>The latest historical market data entry, or null if none found</returns>
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

    /// <summary>
    /// Retrieves the latest historical market data entries for a specific symbol
    /// </summary>
    /// <param name="symbol">The stock symbol to retrieve data for</param>
    /// <param name="count">The number of latest entries to retrieve</param>
    /// <returns>Collection of the latest historical market data entries, or null if none found</returns>
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
    
    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <returns>The created user entity.</returns>
    public async Task<User> CreateUserAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
    
    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive users.</param>
    /// <returns>A collection of users.</returns>
    public async Task<IEnumerable<User>> GetAllUsersAsync(bool includeInactive = false)
    {
        var query = context.Users
            .Include(u => u.Preferences)
            .Include(u => u.PersonalInfo)
            .AsQueryable();
            
        if (!includeInactive)
            query = query.Where(u => u.IsActive);
            
        return await query.ToListAsync();
    }
    
    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">The email address to look for.</param>
    /// <returns>The user if found, null otherwise.</returns>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await context.Users
            .Include(u => u.Preferences)
            .Include(u => u.PersonalInfo)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }
    
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID to look for.</param>
    /// <param name="includeInactive">Whether to include inactive users.</param>
    /// <returns>The user if found, null otherwise.</returns>
    public async Task<User?> GetUserByIdAsync(string id, bool includeInactive = false)
    {
        var query = context.Users
            .Include(u => u.Preferences)
            .Include(u => u.PersonalInfo)
            .Include(u => u.Portfolios)
            .AsQueryable();
            
        if (!includeInactive)
            query = query.Where(u => u.IsActive);
            
        return await query.FirstOrDefaultAsync(u => u.Id == id);
    }
    
    /// <summary>
    /// Updates a user's information.
    /// </summary>
    /// <param name="user">The user entity with updated properties.</param>
    /// <returns>The updated user entity.</returns>
    public async Task<User> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return user;
    }
    
    /// <summary>
    /// Updates a user's active status.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="isActive">The new active status.</param>
    /// <returns>The updated user entity.</returns>
    public async Task<User> UpdateUserActiveStatusAsync(string userId, bool isActive)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");
            
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow.ToLocalTime();
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return user;
    }
    
    /// <summary>
    /// Updates a user's personal information.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="personalInfo">The personal information to update.</param>
    /// <returns>The updated personal information entity.</returns>
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
    
    /// <summary>
    /// Updates a user's preferences.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="preferences">The preferences to update.</param>
    /// <returns>The updated preferences entity.</returns>
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

    /// <summary>
    /// Gets all portfolios in the system
    /// </summary>
    /// <returns>A collection of all portfolios</returns>
    public async Task<IEnumerable<Portfolio>> GetAllPortfoliosAsync()
    {
        return await context.Portfolios
            .Where(p => p.IsActive)
            .Include(p => p.User)
            .ToListAsync();
    }
    
    /// <summary>
    /// Gets all portfolios for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A collection of portfolios.</returns>
    public async Task<IEnumerable<Portfolio>> GetUserPortfoliosAsync(string userId)
    {
        return await context.Portfolios
            .Where(p => p.UserId == userId && p.IsActive)
            .ToListAsync();
    }
    
    /// <summary>
    /// Gets a portfolio by its ID.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>The portfolio if found, null otherwise.</returns>
    public async Task<Portfolio?> GetPortfolioByIdAsync(string portfolioId)
    {
        return await context.Portfolios
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.IsActive);
    }

    /// <summary>
    /// Gets a portfolio by its ID along with its stocks.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>The portfolio with its stocks if found, null otherwise.</returns>
    public async Task<Portfolio?> GetPortfolioByIdWithPortfolioStocksAsync(string portfolioId)
    {
        return await context.Portfolios
            .Include(p => p.PortfolioStocks)
            .FirstOrDefaultAsync(p => p.Id == portfolioId && p.IsActive);
    }
    
    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio entity to create.</param>
    /// <returns>The created portfolio entity.</returns>
    public async Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio)
    {
        portfolio.CreatedAt = DateTime.UtcNow.ToLocalTime();
        portfolio.LastUpdated = DateTime.UtcNow.ToLocalTime();
        context.Portfolios.Add(portfolio);
        await context.SaveChangesAsync();
        return portfolio;
    }
    
    /// <summary>
    /// Updates a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio entity with updated properties.</param>
    /// <returns>The updated portfolio entity.</returns>
    public async Task<Portfolio> UpdatePortfolioAsync(Portfolio portfolio)
    {
        portfolio.LastUpdated = DateTime.UtcNow.ToLocalTime();
        context.Portfolios.Update(portfolio);
        await context.SaveChangesAsync();
        return portfolio;
    }
    
    /// <summary>
    /// Deletes a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    public async Task<bool> DeletePortfolioAsync(string portfolioId)
    {
        var portfolio = await context.Portfolios.FindAsync(portfolioId);
        if (portfolio == null)
            return false;
            
        portfolio.IsActive = false;
        portfolio.LastUpdated = DateTime.UtcNow.ToLocalTime();
        context.Portfolios.Update(portfolio);
        await context.SaveChangesAsync();
        return true;
    }
    
    // Transaction Operations
    
    /// <summary>
    /// Gets all transactions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A collection of transactions.</returns>
    public async Task<IEnumerable<Transaction>> GetUserTransactionsAsync(string userId)
    {
        return await context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
    
    /// <summary>
    /// Gets all transactions for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>A collection of transactions.</returns>
    public async Task<IEnumerable<Transaction>> GetPortfolioTransactionsAsync(string portfolioId)
    {
        return await context.Transactions
            .Where(t => t.PortfolioId == portfolioId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
    
    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="transaction">The transaction entity to create.</param>
    /// <returns>The created transaction entity.</returns>
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
    
    /// <summary>
    /// Gets all portfolio stocks for a specific portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>A collection of portfolio stocks.</returns>
    public async Task<IEnumerable<PortfolioStock>> GetPortfolioStocksAsync(string portfolioId)
    {
        return await context.PortfolioStocks
            .Where(ps => ps.PortfolioId == portfolioId)
            .Include(ps => ps.Portfolio)
            .ToListAsync();
    }
    
    /// <summary>
    /// Gets a portfolio stock by its ID.
    /// </summary>
    /// <param name="id">The portfolio stock ID.</param>
    /// <returns>The portfolio stock if found, null otherwise.</returns>
    public async Task<PortfolioStock?> GetPortfolioStockByIdAsync(string id)
    {
        return await context.PortfolioStocks
            .Include(ps => ps.Portfolio)
            .FirstOrDefaultAsync(ps => ps.ID == id);
    }
    
    /// <summary>
    /// Adds a new stock to a portfolio.
    /// </summary>
    /// <param name="portfolioStock">The portfolio stock entity to create.</param>
    /// <returns>The created portfolio stock entity.</returns>
    public async Task<PortfolioStock> AddPortfolioStockAsync(PortfolioStock portfolioStock)
    {
        context.PortfolioStocks.Add(portfolioStock);
        await context.SaveChangesAsync();
        return portfolioStock;
    }
    
    /// <summary>
    /// Updates a portfolio stock.
    /// </summary>
    /// <param name="portfolioStock">The portfolio stock entity with updated properties.</param>
    /// <returns>The updated portfolio stock entity.</returns>
    public async Task<PortfolioStock> UpdatePortfolioStockAsync(PortfolioStock portfolioStock)
    {
        context.PortfolioStocks.Update(portfolioStock);
        await context.SaveChangesAsync();
        return portfolioStock;
    }
    
    /// <summary>
    /// Deletes a portfolio stock.
    /// </summary>
    /// <param name="id">The portfolio stock ID to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
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
    
    /// <summary>
    /// Gets all companies.
    /// </summary>
    /// <returns>A collection of companies.</returns>
    public async Task<IEnumerable<Company>> GetAllCompaniesAsync()
    {
        return await context.Companies
            .OrderBy(c => c.Symbol)
            .ToListAsync();
    }
    
    /// <summary>
    /// Gets a company by its ID.
    /// </summary>
    /// <param name="id">The company stock ID.</param>
    /// <returns>The company if found, null otherwise.</returns>
    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        return await context.Companies
            .FirstOrDefaultAsync(c => c.StockId == id);
    }
    
    /// <summary>
    /// Gets a company by its stock symbol.
    /// </summary>
    /// <param name="symbol">The stock symbol.</param>
    /// <returns>The company if found, null otherwise.</returns>
    public async Task<Company?> GetCompanyBySymbolAsync(string symbol)
    {
        return await context.Companies
            .FirstOrDefaultAsync(c => c.Symbol == symbol);
    }
    
    /// <summary>
    /// Creates a new company.
    /// </summary>
    /// <param name="company">The company entity to create.</param>
    /// <returns>The created company entity.</returns>
    public async Task<Company> CreateCompanyAsync(Company company)
    {
        company.CreatedAt = DateTime.UtcNow.ToLocalTime();
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        return company;
    }
    
    /// <summary>
    /// Updates a company.
    /// </summary>
    /// <param name="company">The company entity with updated properties.</param>
    /// <returns>The updated company entity.</returns>
    public async Task<Company> UpdateCompanyAsync(Company company)
    {
        context.Companies.Update(company);
        await context.SaveChangesAsync();
        return company;
    }
    
    /// <summary>
    /// Deletes a company.
    /// </summary>
    /// <param name="id">The company stock ID to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
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

    /// <summary>
    /// Creates a new service account
    /// </summary>
    /// <param name="serviceAccount">The service account entity to create</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task CreateServiceAccountAsync(ServiceAccount serviceAccount)
    {
        context.ServiceAccounts.Add(serviceAccount);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a service account by its ID
    /// </summary>
    /// <param name="id">The service account ID</param>
    /// <returns>The service account if found, null otherwise</returns>
    public async Task<ServiceAccount?> GetServiceAccountAsync(string id)
    {
        return await context.ServiceAccounts
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>
    /// Updates an existing service account
    /// </summary>
    /// <param name="id">The service account ID</param>
    /// <param name="account">The updated service account entity</param>
    /// <returns>The updated service account entity</returns>
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

    /// <summary>
    /// Gets a transaction by its ID
    /// </summary>
    /// <param name="id">The transaction ID</param>
    /// <returns>The transaction if found, null otherwise</returns>
    public async Task<Transaction?> GetTransactionByIdAsync(string id)
    {
        return await context.Transactions
            .Include(t => t.Portfolio)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Cancels a transaction by its ID
    /// </summary>
    /// <param name="id">The transaction ID to cancel</param>
    /// <returns>A task representing the asynchronous operation</returns>
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

    /// <summary>
    /// Gets the latest log entries from the system
    /// </summary>
    /// <param name="count">The number of latest log entries to retrieve</param>
    /// <returns>A collection of the latest log entries</returns>
    public async Task<IEnumerable<Log>> GetLatestLogsAsync(int count)
    {
        return await context.LogEvents
            .OrderByDescending(l => l.TimeStamp)
            .Take(count)
            .ToListAsync();
    }
}