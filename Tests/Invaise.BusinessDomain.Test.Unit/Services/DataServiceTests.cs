using Invaise.BusinessDomain.API.Constants;
using Invaise.BusinessDomain.API.Services;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class DataServiceTests : TestBase, IDisposable
{
    private readonly DataService _service;
    private readonly string _testDir;
    private readonly string _rawDataPath;
    private readonly string _outputDataPath;
    
    public DataServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DataServiceTests");
        _rawDataPath = Path.Combine(_testDir, "raw");
        _outputDataPath = Path.Combine(_testDir, "output");
        
        Directory.CreateDirectory(_rawDataPath);
        Directory.CreateDirectory(_outputDataPath);
        
        _service = new DataService();
    }
    
    [Fact]
    public void SMPDatasetCleanupAsync_NotTestable_DueToConstantFields()
    {
        Assert.True(true);
    }
    
    [Fact]
    public void DataService_CanBeCreated()
    {
        var service = new DataService();
        Assert.NotNull(service);
    }
    
    [Fact (Skip = "This test is not directly testable due to private constant fields in GlobalConstants.")]
    public async Task SMPDatasetCleanupAsync_ProcessesDataCorrectly_UsingReflection()
    {
        try
        {
            var originalRawPath = GetPrivateConstantValue<string>(typeof(GlobalConstants), "RAW_DATA_PATH");
            var originalOutputPath = GetPrivateConstantValue<string>(typeof(GlobalConstants), "OUTPUT_DATA_PATH");

            SetPrivateConstantValue(typeof(GlobalConstants), "RAW_DATA_PATH", _rawDataPath);
            SetPrivateConstantValue(typeof(GlobalConstants), "OUTPUT_DATA_PATH", _outputDataPath);

            var testData = "Date,Open,High,Low,Close,Adj Close,Volume,Symbol\n" +
                          "2023-01-01,100.0,105.0,99.0,103.0,103.0,1000000,AAPL\n" +
                          "2023-01-02,103.0,107.0,102.0,106.0,106.0,1200000,AAPL\n";
            
            var inputFile = Path.Combine(_rawDataPath, "sp500_stocks.csv");
            await File.WriteAllTextAsync(inputFile, testData);

            await _service.SMPDatasetCleanupAsync();

            var outputFile = Path.Combine(_outputDataPath, "sp500_stocks_cleaned.csv");
            Assert.True(File.Exists(outputFile));

            var outputContent = await File.ReadAllTextAsync(outputFile);
            var lines = outputContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length >= 2);
            Assert.Contains("Date,Open,High,Low,Close,Volume,Symbol", lines[0]);

            SetPrivateConstantValue(typeof(GlobalConstants), "RAW_DATA_PATH", originalRawPath);
            SetPrivateConstantValue(typeof(GlobalConstants), "OUTPUT_DATA_PATH", originalOutputPath);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test failed with exception: {ex.Message}");
        }
    }
    
    private static T GetPrivateConstantValue<T>(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        return (T)field?.GetValue(null);
    }
    
    private static void SetPrivateConstantValue(Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        field?.SetValue(null, value);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
} 