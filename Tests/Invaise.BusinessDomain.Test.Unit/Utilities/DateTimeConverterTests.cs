using Invaise.BusinessDomain.API.Utils;

namespace Invaise.BusinessDomain.Test.Unit.Utilities;

public class DateTimeConverterTests
{
    [Fact]
    public void UnixTimestampToDateTime_ReturnsCorrectDateTime()
    {
        // Arrange
        long unixTimestamp = 1609459200; // 2021-01-01 00:00:00 UTC

        // Act
        DateTime result = DateTimeConverter.UnixTimestampToDateTime(unixTimestamp);

        // Assert
        DateTime expected = DateTime.UnixEpoch.AddSeconds(unixTimestamp).ToLocalTime();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)] // Unix epoch (1970-01-01 00:00:00 UTC)
    [InlineData(1577836800)] // 2020-01-01 00:00:00 UTC
    [InlineData(1672531200)] // 2023-01-01 00:00:00 UTC
    public void UnixTimestampToDateTime_WithVariousTimestamps_ConvertsCorrectly(long timestamp)
    {
        // Act
        DateTime result = DateTimeConverter.UnixTimestampToDateTime(timestamp);

        // Assert
        DateTime expected = DateTime.UnixEpoch.AddSeconds(timestamp).ToLocalTime();
        Assert.Equal(expected, result);
    }
} 