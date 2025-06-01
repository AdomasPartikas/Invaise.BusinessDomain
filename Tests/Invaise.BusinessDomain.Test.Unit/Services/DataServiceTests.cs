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
    private readonly string _dataPath;
    
    public DataServiceTests()
    {
        // Create the expected directory structure that DataService looks for
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _dataPath = Path.GetFullPath(Path.Combine(baseDirectory, GlobalConstants.DataFolder));
        
        Directory.CreateDirectory(_dataPath);
        
        _service = new DataService();
    }
    
    [Fact]
    public void DataService_CanBeCreated()
    {
        var service = new DataService();
        Assert.NotNull(service);
    }

    [Fact]
    public void DataService_ImplementsIDataService()
    {
        var service = new DataService();
        Assert.IsAssignableFrom<IDataService>(service);
    }

    [Fact]
    public async Task SMPDatasetCleanupAsync_ThrowsException_WhenFileIsTooShort()
    {
        // Arrange
        var testData = "Header1,Header2\nRow1,Data1\nRow2,Data2"; // Only 3 rows, need at least 4
        var inputFile = Path.Combine(_dataPath, GlobalConstants.SmpDatasetRaw);
        await File.WriteAllTextAsync(inputFile, testData);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SMPDatasetCleanupAsync());
        
        Assert.Contains("CSV is too short or not in expected format", exception.Message);
    }

    [Fact]
    public async Task SMPDatasetCleanupAsync_ThrowsException_WhenInputFileDoesNotExist()
    {
        // Arrange - ensure no input file exists
        var inputFile = Path.Combine(_dataPath, GlobalConstants.SmpDatasetRaw);
        if (File.Exists(inputFile))
        {
            File.Delete(inputFile);
        }

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.SMPDatasetCleanupAsync());
    }

    [Fact]
    public void TryParseDecimal_ReturnsNull_WhenValueIsNull()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object?[] { null });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryParseDecimal_ReturnsNull_WhenValueIsEmpty()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object[] { string.Empty });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryParseDecimal_ReturnsNull_WhenValueIsInvalid()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object[] { "invalid_decimal" });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryParseDecimal_ReturnsValue_WhenValueIsValid()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object[] { "123.45" });

        // Assert
        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void TryParseDecimal_ReturnsValue_WhenValueIsInteger()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object[] { "42" });

        // Assert
        Assert.Equal(42m, result);
    }

    [Fact]
    public void TryParseDecimal_ReturnsValue_WhenValueIsNegative()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object[] { "-15.75" });

        // Assert
        Assert.Equal(-15.75m, result);
    }

    [Fact]
    public void TryParseDecimal_ReturnsValue_WhenValueIsZero()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object[] { "0" });

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void TryParseDecimal_ReturnsValue_WhenValueHasWhitespace()
    {
        // Arrange
        var service = new DataService();
        var method = typeof(DataService).GetMethod("TryParseDecimal", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = method!.Invoke(service, new object[] { "  123.45  " });

        // Assert
        Assert.Equal(123.45m, result);
    }

    [Fact]
    public async Task SMPDatasetCleanupAsync_CreatesOutputFile_WhenInputIsValid()
    {
        // Arrange
        var testData = "Date,Header1,Header2\n" + // Row 0
                      "Symbol,AAPL,MSFT\n" + // Row 1 - tickers
                      "Empty,Row,Data\n" + // Row 2
                      "2023-01-01,100.0,200.0,110.0,210.0,90.0,190.0,95.0,195.0,1000,2000\n"; // Row 3 - data
        
        var inputFile = Path.Combine(_dataPath, GlobalConstants.SmpDatasetRaw);
        await File.WriteAllTextAsync(inputFile, testData);

        // Act
        await _service.SMPDatasetCleanupAsync();

        // Assert
        var outputFile = Path.Combine(_dataPath, GlobalConstants.SmpDataset);
        Assert.True(File.Exists(outputFile));
    }

    [Fact]
    public async Task SMPDatasetCleanupAsync_SkipsRowsWithInvalidDate()
    {
        // Arrange
        var testData = "Date,Header1,Header2\n" + // Row 0
                      "Symbol,AAPL,MSFT\n" + // Row 1 - tickers
                      "Empty,Row,Data\n" + // Row 2
                      "invalid-date,100.0,200.0,110.0,210.0,90.0,190.0,95.0,195.0,1000,2000\n" + // Row 3 - invalid date
                      "2023-01-01,100.0,200.0,110.0,210.0,90.0,190.0,95.0,195.0,1000,2000\n"; // Row 4 - valid data
        
        var inputFile = Path.Combine(_dataPath, GlobalConstants.SmpDatasetRaw);
        await File.WriteAllTextAsync(inputFile, testData);

        // Act
        await _service.SMPDatasetCleanupAsync();

        // Assert
        var outputFile = Path.Combine(_dataPath, GlobalConstants.SmpDataset);
        Assert.True(File.Exists(outputFile));
        var outputContent = await File.ReadAllTextAsync(outputFile);
        Assert.DoesNotContain("invalid-date", outputContent);
    }

    [Fact]
    public async Task SMPDatasetCleanupAsync_SkipsRowsWithInsufficientColumns()
    {
        // Arrange
        var testData = "Date,Header1,Header2\n" + // Row 0
                      "Symbol,AAPL,MSFT\n" + // Row 1 - tickers
                      "Empty,Row,Data\n" + // Row 2
                      "2023-01-01,100.0\n" + // Row 3 - insufficient columns
                      "2023-01-02,100.0,200.0,110.0,210.0,90.0,190.0,95.0,195.0,1000,2000\n"; // Row 4 - valid data
        
        var inputFile = Path.Combine(_dataPath, GlobalConstants.SmpDatasetRaw);
        await File.WriteAllTextAsync(inputFile, testData);

        // Act
        await _service.SMPDatasetCleanupAsync();

        // Assert
        var outputFile = Path.Combine(_dataPath, GlobalConstants.SmpDataset);
        Assert.True(File.Exists(outputFile));
        var outputContent = await File.ReadAllTextAsync(outputFile);
        var lines = outputContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 1);
    }

    public void Dispose()
    {
        var inputFile = Path.Combine(_dataPath, GlobalConstants.SmpDatasetRaw);
        var outputFile = Path.Combine(_dataPath, GlobalConstants.SmpDataset);
        
        if (File.Exists(inputFile))
        {
            File.Delete(inputFile);
        }
        
        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }
    }
} 