using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Interfaces;

/// <summary>
/// Represents a service for interacting with the database
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Retrieves all unique market data symbols from the database asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an enumerable collection of unique market data symbols.</returns>
    Task<IEnumerable<string>> GetAllUniqueMarketDataSymbolsAsync();

    /// <summary>
    /// Retrieves intraday market data for a specific symbol within an optional date range asynchronously.
    /// </summary>
    Task<IEnumerable<IntradayMarketData>> GetIntradayMarketDataAsync(string symbol, DateTime? start, DateTime? end);

    Task<IntradayMarketData?> GetLatestIntradayMarketDataAsync(string symbol);

    Task<IEnumerable<IntradayMarketData>?> GetLatestIntradayMarketDataAsync(string symbol, int count);

    /// <summary>
    /// Retrieves market data for a specific symbol within an optional date range asynchronously.
    /// </summary>
    /// <param name="symbol">The market data symbol to retrieve data for.</param>
    /// <param name="start">The optional start date for the data range. If null, no start date filter is applied.</param>
    /// <param name="end">The optional end date for the data range. If null, no end date filter is applied.</param>
    /// <returns>A task that represents the asynchronous operation. 
    /// The task result contains an enumerable collection of market data for the specified symbol and date range.</returns>
    Task<IEnumerable<HistoricalMarketData>> GetHistoricalMarketDataAsync(string symbol, DateTime? start, DateTime? end);

    Task<HistoricalMarketData?> GetLatestHistoricalMarketDataAsync(string symbol);

    Task<IEnumerable<HistoricalMarketData>?> GetLatestHistoricalMarketDataAsync(string symbol, int count);

    /// <summary>
    /// Creates a new user in the database.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <returns>The created user entity.</returns>
    Task<User> CreateUserAsync(User user);

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive users.</param>
    /// <returns>A collection of users.</returns>
    Task<IEnumerable<User>> GetAllUsersAsync(bool includeInactive = false);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">The email address to look for.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID to look for.</param>
    /// <param name="includeInactive">Whether to include inactive users.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetUserByIdAsync(string id, bool includeInactive = false);

    /// <summary>
    /// Updates a user's information.
    /// </summary>
    /// <param name="user">The user entity with updated properties.</param>
    /// <returns>The updated user entity.</returns>
    Task<User> UpdateUserAsync(User user);

    /// <summary>
    /// Updates a user's active status.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="isActive">The new active status.</param>
    /// <returns>The updated user entity.</returns>
    Task<User> UpdateUserActiveStatusAsync(string userId, bool isActive);

    /// <summary>
    /// Updates a user's personal information.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="personalInfo">The personal information to update.</param>
    /// <returns>The updated personal information entity.</returns>
    Task<UserPersonalInfo> UpdateUserPersonalInfoAsync(string userId, UserPersonalInfo personalInfo);

    /// <summary>
    /// Updates a user's preferences.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="preferences">The preferences to update.</param>
    /// <returns>The updated preferences entity.</returns>
    Task<UserPreferences> UpdateUserPreferencesAsync(string userId, UserPreferences preferences);

    Task<IEnumerable<Portfolio>> GetAllPortfoliosAsync();

    /// <summary>
    /// Gets all portfolios for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A collection of portfolios.</returns>
    Task<IEnumerable<Portfolio>> GetUserPortfoliosAsync(string userId);

    /// <summary>
    /// Gets a portfolio by its ID.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>The portfolio if found, null otherwise.</returns>
    Task<Portfolio?> GetPortfolioByIdAsync(string portfolioId);

    /// <summary>
    /// Gets a portfolio by its ID along with its stocks.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>The portfolio with its stocks if found, null otherwise.</returns>
    Task<Portfolio?> GetPortfolioByIdWithPortfolioStocksAsync(string portfolioId);

    /// <summary>
    /// Creates a new portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio entity to create.</param>
    /// <returns>The created portfolio entity.</returns>
    Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio);

    /// <summary>
    /// Updates a portfolio.
    /// </summary>
    /// <param name="portfolio">The portfolio entity with updated properties.</param>
    /// <returns>The updated portfolio entity.</returns>
    Task<Portfolio> UpdatePortfolioAsync(Portfolio portfolio);

    /// <summary>
    /// Deletes a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    Task<bool> DeletePortfolioAsync(string portfolioId);

    /// <summary>
    /// Gets all transactions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A collection of transactions.</returns>
    Task<IEnumerable<Transaction>> GetUserTransactionsAsync(string userId);

    /// <summary>
    /// Gets all transactions for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>A collection of transactions.</returns>
    Task<IEnumerable<Transaction>> GetPortfolioTransactionsAsync(string portfolioId);

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="transaction">The transaction entity to create.</param>
    /// <returns>The created transaction entity.</returns>
    Task<Transaction> CreateTransactionAsync(Transaction transaction);

    /// <summary>
    /// Gets all portfolio stocks for a specific portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>A collection of portfolio stocks.</returns>
    Task<IEnumerable<PortfolioStock>> GetPortfolioStocksAsync(string portfolioId);
    
    /// <summary>
    /// Gets a portfolio stock by its ID.
    /// </summary>
    /// <param name="id">The portfolio stock ID.</param>
    /// <returns>The portfolio stock if found, null otherwise.</returns>
    Task<PortfolioStock?> GetPortfolioStockByIdAsync(string id);
    
    /// <summary>
    /// Adds a new stock to a portfolio.
    /// </summary>
    /// <param name="portfolioStock">The portfolio stock entity to create.</param>
    /// <returns>The created portfolio stock entity.</returns>
    Task<PortfolioStock> AddPortfolioStockAsync(PortfolioStock portfolioStock);
    
    /// <summary>
    /// Updates a portfolio stock.
    /// </summary>
    /// <param name="portfolioStock">The portfolio stock entity with updated properties.</param>
    /// <returns>The updated portfolio stock entity.</returns>
    Task<PortfolioStock> UpdatePortfolioStockAsync(PortfolioStock portfolioStock);
    
    /// <summary>
    /// Deletes a portfolio stock.
    /// </summary>
    /// <param name="id">The portfolio stock ID to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    Task<bool> DeletePortfolioStockAsync(string id);
    
    /// <summary>
    /// Gets all companies.
    /// </summary>
    /// <returns>A collection of companies.</returns>
    Task<IEnumerable<Company>> GetAllCompaniesAsync();
    
    /// <summary>
    /// Gets a company by its ID.
    /// </summary>
    /// <param name="id">The company stock ID.</param>
    /// <returns>The company if found, null otherwise.</returns>
    Task<Company?> GetCompanyByIdAsync(int id);
    
    /// <summary>
    /// Gets a company by its stock symbol.
    /// </summary>
    /// <param name="symbol">The stock symbol.</param>
    /// <returns>The company if found, null otherwise.</returns>
    Task<Company?> GetCompanyBySymbolAsync(string symbol);
    
    /// <summary>
    /// Creates a new company.
    /// </summary>
    /// <param name="company">The company entity to create.</param>
    /// <returns>The created company entity.</returns>
    Task<Company> CreateCompanyAsync(Company company);
    
    /// <summary>
    /// Updates a company.
    /// </summary>
    /// <param name="company">The company entity with updated properties.</param>
    /// <returns>The updated company entity.</returns>
    Task<Company> UpdateCompanyAsync(Company company);
    
    /// <summary>
    /// Deletes a company.
    /// </summary>
    /// <param name="id">The company stock ID to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    Task<bool> DeleteCompanyAsync(int id);

    //For service account

    Task<ServiceAccount?> GetServiceAccountAsync(string id);
    Task CreateServiceAccountAsync(ServiceAccount serviceAccount);
    Task<ServiceAccount> UpdateServiceAccountAsync(string id, ServiceAccount account);
    Task<Transaction?> GetTransactionByIdAsync(string id);
    Task CancelTransactionAsync(string id);
    Task<IEnumerable<Log>> GetLatestLogsAsync(int count);
}