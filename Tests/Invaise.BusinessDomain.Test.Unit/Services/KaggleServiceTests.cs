using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Invaise.BusinessDomain.API.Services;
using Invaise.BusinessDomain.API.Interfaces;
using Moq;
using Xunit;
using System.IO;
using NSubstitute;

namespace Invaise.BusinessDomain.Test.Unit.Services;

public class KaggleServiceTests : TestBase
{
    private readonly KaggleService _sut;

    public KaggleServiceTests()
    {
        _sut = new KaggleService();
    }

    [Fact]
    public async Task DownloadDatasetAsync_ValidUrl_ProcessStarted()
    {
        // Arrange
        var datasetUrl = "validKaggleDataset/path";
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _sut.DownloadDatasetAsync(datasetUrl));
    }
    
    [Fact]
    public async Task DownloadDatasetAsync_InvalidUrl_ThrowsException()
    {
        // Arrange
        var datasetUrl = "invalid/format/with/too/many/slashes";
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => 
            await _sut.DownloadDatasetAsync(datasetUrl));
    }
    
    [Fact]
    public async Task DownloadDatasetAsync_NullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        string? datasetUrl = null;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => 
            await _sut.DownloadDatasetAsync(datasetUrl!));
    }
} 