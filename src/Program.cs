using Invaise.BusinessDomain.API.FinnhubAPIClient;
using Invaise.BusinessDomain.API.GaiaAPIClient;
using Invaise.BusinessDomain.API.ApolloAPIClient;
using Invaise.BusinessDomain.API.Context;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;
using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.MariaDB.Extensions;
using Serilog.Sinks.MariaDB;


// Enable Serilog self-logging to see internal errors
SelfLog.Enable(msg => Console.WriteLine($"Serilog Internal Error: {msg}"));

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var sinkOpts = new MariaDBSinkOptions
    {
        PropertiesToColumnsMapping = new()
        {
            ["Timestamp"] = "Timestamp",
            ["Level"] = "Level",
            ["Message"] = "Message",
            ["MessageTemplate"] = "MessageTemplate",
            ["Exception"] = "Exception",
            ["Properties"] = "Properties",
            ["CorrelationId"] = "CorrelationId",
            ["ProcessId"] = "ProcessId",
            ["ProcessName"] = "ProcessName",
            ["ClientIp"] = "ClientIp"
        },
        ExcludePropertiesWithDedicatedColumn = true
        //EnumsAsInts = true
    };

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Hangfire", Serilog.Events.LogEventLevel.Error)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        //.Enrich.WithProperty("LogLevel", Serilog.Events.LogEventLevel.Information.ToString())
        .Enrich.WithCorrelationId()
        .Enrich.WithProcessId()
        .Enrich.WithProcessName()
        .Enrich.WithClientIp()
        .WriteTo.Console()
        .WriteTo.MariaDB(
            connectionString: builder.Configuration.GetConnectionString("InvaiseDB"),
            tableName: "LogEvents",
            autoCreateTable: false,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
            batchPostingLimit: 1000,
            period: TimeSpan.FromSeconds(5),
            options: sinkOpts
            )
        .CreateLogger();

    builder.Host.UseSerilog(Log.Logger);

    // Log startup configuration
    Log.Debug("Starting application configuration");
    Log.Debug("Connection string: {ConnectionString}", builder.Configuration.GetConnectionString("InvaiseDB"));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin",
            policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
    });

    builder.Services.AddDbContext<InvaiseDbContext>(options =>
        options.UseMySql(
            builder.Configuration.GetConnectionString("InvaiseDB"),
            new MySqlServerVersion(new Version(8, 0, 39))
        )
    );

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();


    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Invaise.BusinessDomain", Version = "v1" });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    builder.Services.AddHttpClient<IFinnhubClient, FinnhubClient>(client =>
    {
        client.DefaultRequestHeaders.Add("X-Finnhub-Token", builder.Configuration["FinnhubKey"]);
        client.BaseAddress = new Uri(builder.Configuration["FinnhubBaseUrl"] ?? throw new InvalidOperationException("Finnhub base URL not configured"));
    });

    builder.Services.AddHttpClient<IHealthGaiaClient, HealthGaiaClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Gaia:BaseUrl"] ?? throw new InvalidOperationException("Gaia base URL not configured"));
    });

    builder.Services.AddScoped<IKaggleService, KaggleService>();
    builder.Services.AddScoped<IDataService, DataService>();
    builder.Services.AddScoped<IMarketDataService, MarketDataService>();
    builder.Services.AddScoped<IDatabaseService, DatabaseService>();

    builder.Services.AddHangfire(config =>
    {
        config.UseMemoryStorage();
    });

    builder.Services.AddHangfireServer();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var marketDataService = scope.ServiceProvider.GetRequiredService<IMarketDataService>();

        Log.Debug("Starting initial data import... Please wait.");

        //await marketDataService.FetchAndImportHistoricalMarketDataAsync();
        //await marketDataService.ImportCompanyDataAsync();

        Log.Debug("Initial data import completed.");
    }

    app.UseHangfireDashboard();

#pragma warning disable CS0618 // Type or member is obsolete
    app.UseHangfireServer();
#pragma warning restore CS0618 // Type or member is obsolete

    RecurringJob.AddOrUpdate<IMarketDataService>(
        "refresh-historical-data",
        service => service.FetchAndImportHistoricalMarketDataAsync(),
        "0 0 * * *");

    RecurringJob.AddOrUpdate<IMarketDataService>(
        "refresh-intraday-data",
        service => service.ImportIntradayMarketDataAsync(),
        "*/5 * * * *");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowSpecificOrigin");

    app.UseRouting();

    app.MapControllers();

    app.UseSwagger();
    app.UseSwaggerUI();

    Log.Information("Application started successfully.");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
