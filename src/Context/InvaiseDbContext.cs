using Microsoft.EntityFrameworkCore;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Context;

/// <summary>
/// Initializes a new instance of the <see cref="InvaiseDbContext"/> class with the specified options.
/// </summary>
/// <param name="options">The options to configure the database context.</param>
public class InvaiseDbContext(DbContextOptions<InvaiseDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the collection of users in the database.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;
    /// <summary>
    /// Gets or sets the collection of user roles in the database.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    /// <summary>
    /// Gets or sets the collection of user personal information in the database.
    /// </summary>
    public DbSet<UserPersonalInfo> UserPersonalInfos { get; set; } = null!;

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


    /// <summary>
    /// Configures the model for the database context.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for the database context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleName });

        modelBuilder.Entity<UserPersonalInfo>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<UserPersonalInfo>(pi => pi.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HistoricalMarketData>(entity => entity.HasIndex(e => new { e.Symbol, e.Date })
            .IsUnique());

        modelBuilder.Entity<IntradayMarketData>(entity => entity.HasIndex(e => new { e.Symbol, e.Timestamp })
            .IsUnique());

        modelBuilder.Entity<Portfolio>()
            .HasOne(p => p.User)
            .WithMany(u => u.Portfolios)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PortfolioStock>()
            .HasOne(ps => ps.Portfolio)
            .WithMany(p => p.PortfolioStocks)
            .HasForeignKey("PortfolioId");

        modelBuilder.Entity<PortfolioStock>()
            .HasOne(ps => ps.Company)
            .WithMany()
            .HasForeignKey("CompanyId");

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Portfolio)
            .WithMany()
            .HasForeignKey("PortfolioId");

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey("CompanyId");

        modelBuilder.Entity<PortfolioHealth>()
            .HasOne(ph => ph.Portfolio)
            .WithMany()
            .HasForeignKey("PortfolioId");

        modelBuilder.Entity<Log>()
            .ToTable("LogEvents")
            .HasKey(l => l.Id);
    }
}
