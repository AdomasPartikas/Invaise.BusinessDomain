using Invaise.BusinessDomain.API.FinnhubAPIClient;
using Invaise.BusinessDomain.API.GaiaAPIClient;
using Invaise.BusinessDomain.API.ApolloAPIClient;
using Invaise.BusinessDomain.API.IgnisAPIClient;
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
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Invaise.BusinessDomain.API.Middleware;
using AutoMapper;
using System.Configuration;
using Invaise.BusinessDomain.API.Config;
using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;


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
                policy.WithOrigins(builder.Configuration["UIBaseUrl"] ?? throw new InvalidOperationException("UIBaseUrl not configured"))
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

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            options.JsonSerializerOptions.MaxDepth = 64;
        })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.Converters.Add(new StringEnumConverter());
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        });

    builder.Services.AddEndpointsApiExplorer();


    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Invaise.BusinessDomain", Version = "v1" });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
        
        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });

    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    // Add authentication services
    var jwtKey = builder.Configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key not configured");
    var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer not configured");
    var jwtAudience = builder.Configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience not configured");
    
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.Zero
        };
    });

    // Add our custom services
    builder.Services.AddScoped<IAuthService, AuthService>();

    builder.Services.AddHttpClient<IFinnhubClient, FinnhubClient>(client =>
    {
        client.DefaultRequestHeaders.Add("X-Finnhub-Token", builder.Configuration["FinnhubKey"]);
        client.BaseAddress = new Uri(builder.Configuration["FinnhubBaseUrl"] ?? throw new InvalidOperationException("Finnhub base URL not configured"));
    });

    // Gaia Client Registration
    builder.Services.AddHttpClient<IHealthGaiaClient, HealthGaiaClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Gaia:BaseUrl"] ?? throw new InvalidOperationException("Gaia base URL not configured"));
    });
    builder.Services.AddHttpClient<IPredictGaiaClient, PredictGaiaClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Gaia:BaseUrl"] ?? throw new InvalidOperationException("Gaia base URL not configured"));
    });
    builder.Services.AddHttpClient<IOptimizeGaiaClient, OptimizeGaiaClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Gaia:BaseUrl"] ?? throw new InvalidOperationException("Gaia base URL not configured"));
    });
    builder.Services.AddHttpClient<IWeightsGaiaClient, WeightsGaiaClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Gaia:BaseUrl"] ?? throw new InvalidOperationException("Gaia base URL not configured"));
    });

    builder.Services.AddHttpClient<IHealthApolloClient, HealthApolloClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Apollo:BaseUrl"] ?? throw new InvalidOperationException("Apollo base URL not configured"));
    });
    builder.Services.AddHttpClient<IPredictApolloClient, PredictApolloClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Apollo:BaseUrl"] ?? throw new InvalidOperationException("Apollo base URL not configured"));
    });
    builder.Services.AddHttpClient<IInfoApolloClient, InfoApolloClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Apollo:BaseUrl"] ?? throw new InvalidOperationException("Apollo base URL not configured"));
    });
    builder.Services.AddHttpClient<ITrainApolloClient, TrainApolloClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Apollo:BaseUrl"] ?? throw new InvalidOperationException("Apollo base URL not configured"));
    });
    builder.Services.AddHttpClient<IStatusApolloClient, StatusApolloClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Apollo:BaseUrl"] ?? throw new InvalidOperationException("Apollo base URL not configured"));
    });

    builder.Services.AddHttpClient<IHealthIgnisClient, HealthIgnisClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Ignis:BaseUrl"] ?? throw new InvalidOperationException("Ignis base URL not configured"));
    });
    builder.Services.AddHttpClient<IPredictIgnisClient, PredictIgnisClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Ignis:BaseUrl"] ?? throw new InvalidOperationException("Ignis base URL not configured"));
    });
    builder.Services.AddHttpClient<IInfoIgnisClient, InfoIgnisClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Ignis:BaseUrl"] ?? throw new InvalidOperationException("Ignis base URL not configured"));
    });
    builder.Services.AddHttpClient<ITrainIgnisClient, TrainIgnisClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Ignis:BaseUrl"] ?? throw new InvalidOperationException("Ignis base URL not configured"));
    });
    builder.Services.AddHttpClient<IStatusIgnisClient, StatusIgnisClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Ignis:BaseUrl"] ?? throw new InvalidOperationException("Ignis base URL not configured"));
    });

    builder.Services.AddScoped<IKaggleService, KaggleService>();
    builder.Services.AddScoped<IDataService, DataService>();
    builder.Services.AddScoped<IMarketDataService, MarketDataService>();
    builder.Services.AddScoped<IDatabaseService, DatabaseService>();
    builder.Services.AddScoped<IPortfolioService, PortfolioService>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();

    // Register AI model services
    builder.Services.Configure<AIModelSettings>(builder.Configuration.GetSection("AIModels"));
    
    builder.Services.AddScoped<IGaiaService, GaiaService>();
    builder.Services.AddScoped<IApolloService, ApolloService>();
    builder.Services.AddScoped<IIgnisService, IgnisService>();
    builder.Services.AddScoped<IModelPredictionService, ModelPredictionService>();
    builder.Services.AddScoped<IPortfolioOptimizationService, PortfolioOptimizationService>();

    // Register utility services - these will be implemented next
    builder.Services.AddScoped<IAIModelService, AIModelService>();
    builder.Services.AddScoped<IModelHealthService, ModelHealthService>();
    builder.Services.AddScoped<IModelPerformanceService, ModelPerformanceService>();
    
    // Register email service
    builder.Services.AddScoped<IEmailService, EmailService>();

    builder.Services.AddHangfire(config =>
    {
        config.UseMemoryStorage();
    });

    builder.Services.AddHangfireServer();

    // Configure HTTP clients for AI model services
    builder.Services.AddHttpClient("GaiaClient", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["AIModels:Gaia:BaseUrl"] ?? throw new InvalidOperationException("Gaia base URL not configured"));
    });
    
    // Add a simple HttpClient for direct Gaia calls
    builder.Services.AddHttpClient();

    // Add QuestPDF settings
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var marketDataService = scope.ServiceProvider.GetRequiredService<IMarketDataService>();

        Log.Debug("Starting initial data import... Please wait.");

        await marketDataService.FetchAndImportHistoricalMarketDataAsync();
        await marketDataService.ImportCompanyDataAsync();

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

    RecurringJob.AddOrUpdate<IModelHealthService>(
        "check-model-health",
        service => service.CheckAllModelsHealthAsync(),
        "*/1 * * * *");

    RecurringJob.AddOrUpdate<IModelPerformanceService>(
        "check-model-training-status",
        service => service.CheckTrainingModelsStatusAsync(),
        "*/3 * * * *");
        
    RecurringJob.AddOrUpdate<IModelPerformanceService>(
        "check-model-retraining-needs",
        service => service.CheckAndInitiateRetrainingForAllModelsAsync(),
        "0 */12 * * *");

    RecurringJob.AddOrUpdate<IPortfolioService>(
        "refresh-portfolios",
        service => service.RefreshAllPortfoliosAsync(),
        "*/5 * * * *");
        
    RecurringJob.AddOrUpdate<ITransactionService>(
        "process-pending-transactions",
        service => service.ProcessPendingTransactionsAsync(),
        "*/3 * * * *");

    RecurringJob.AddOrUpdate<IPortfolioOptimizationService>(
        "ensure-inprogress-optimizations",
        service => service.EnsureCompletionOfAllInProgressOptimizationsAsync(),
        "*/3 * * * *");
        
    RecurringJob.AddOrUpdate<IPortfolioService>(
        "save-eod-portfolio-performance",
        service => service.SaveEodPortfolioPerformanceAsync(),
        "0 0 * * *"); // Run daily at midnight
        
    RecurringJob.AddOrUpdate<IModelPredictionService>(
        "update-predictions",
        service => service.RefreshAllPredictionsAsync(),
        "0 */1 * * *");
        

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowSpecificOrigin");

    app.UseRouting();

    // Add JWT middleware
    app.UseMiddleware<JwtMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

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
