using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Context;

/// <summary>
/// Database context for the Invaise application.
/// </summary>
public class InvaiseDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvaiseDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options to configure the database context.</param>
    public InvaiseDbContext(DbContextOptions<InvaiseDbContext> options) : base(options) { }

    /// <summary>
    /// Gets or sets the collection of users in the database.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the collection of user personal information in the database.
    /// </summary>
    public DbSet<UserPersonalInfo> UserPersonalInfo { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the collection of user preferences in the database.
    /// </summary>
    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of market data in the database.
    /// </summary>
    public DbSet<HistoricalMarketData> HistoricalMarketData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of intraday market data in the database.
    /// </summary>
    public DbSet<IntradayMarketData> IntradayMarketData { get; set; } = null!;

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Portfolio> Portfolios { get; set; } = null!;
    public DbSet<PortfolioStock> PortfolioStocks { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<PortfolioHealth> PortfolioHealth { get; set; } = null!;
    public DbSet<Log> LogEvents { get; set; } = null!;
    public DbSet<AIModel> AIModels { get; set; } = null!;
    public DbSet<Prediction> Predictions { get; set; } = null!;
    public DbSet<Heat> Heats { get; set; } = null!;

    public DbSet<ServiceAccount> ServiceAccounts { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the collection of portfolio optimizations in the database.
    /// </summary>
    public DbSet<PortfolioOptimization> PortfolioOptimizations { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the collection of portfolio optimization recommendations in the database.
    /// </summary>
    public DbSet<PortfolioOptimizationRecommendation> PortfolioOptimizationRecommendations { get; set; } = null!;

    /// <summary>
    /// Configures the model for the database context.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for the database context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure User related relationships
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });
        
        modelBuilder.Entity<UserPersonalInfo>()
            .HasOne(pi => pi.User)
            .WithOne(u => u.PersonalInfo)
            .HasForeignKey<UserPersonalInfo>(pi => pi.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<UserPreferences>()
            .HasOne(p => p.User)
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure market data indexes
        modelBuilder.Entity<HistoricalMarketData>(entity => 
            entity.HasIndex(e => new { e.Symbol, e.Date }).IsUnique());

        modelBuilder.Entity<IntradayMarketData>(entity => 
            entity.HasIndex(e => new { e.Symbol, e.Timestamp }).IsUnique());

        // Configure Portfolio related relationships
        modelBuilder.Entity<Portfolio>()
            .HasOne(p => p.User)
            .WithMany(u => u.Portfolios)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PortfolioStock>()
            .HasOne(ps => ps.Portfolio)
            .WithMany(p => p.PortfolioStocks)
            .HasForeignKey(ps => ps.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Transaction relationships
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Portfolio)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PortfolioHealth>()
            .HasOne(ph => ph.Portfolio)
            .WithMany()
            .HasForeignKey("PortfolioId");

        modelBuilder.Entity<Log>()
            .ToTable("LogEvents")
            .HasKey(l => l.Id);

        // Configure PortfolioOptimization relationships
        modelBuilder.Entity<PortfolioOptimization>()
            .HasOne(po => po.Portfolio)
            .WithMany()
            .HasForeignKey(po => po.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PortfolioOptimizationRecommendation>()
            .HasOne(r => r.Optimization)
            .WithMany(o => o.Recommendations)
            .HasForeignKey(r => r.OptimizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}