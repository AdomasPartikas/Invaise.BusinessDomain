using BusinessDomain.FinnhubAPIClient;
using Invaise.BusinessDomain.API.Context;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Interfaces;
using Invaise.BusinessDomain.API.Services;

var builder = WebApplication.CreateBuilder(args);

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
    client.BaseAddress = new Uri(GlobalConstants.FinnhubUrl);
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

    //await marketDataService.FetchAndImportMarketDataAsync();
    Console.WriteLine("SMP Dataset Cleanup Completed.");

    await marketDataService.ImportCompanyDataAsync();
    Console.WriteLine("Company Data Import Completed.");
}

app.UseHangfireDashboard();

#pragma warning disable CS0618 // Type or member is obsolete
app.UseHangfireServer();
#pragma warning restore CS0618 // Type or member is obsolete

RecurringJob.AddOrUpdate<IMarketDataService>(
    "refresh-market-data",
    service => service.FetchAndImportMarketDataAsync(),
    "0 0 * * *");

app.UseCors("AllowSpecificOrigin");

app.UseRouting();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();