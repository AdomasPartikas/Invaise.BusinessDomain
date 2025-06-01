namespace Invaise.BusinessDomain.API.Utils;

/// <summary>
/// Provides utility methods for converting date and time values.
/// </summary>
public static class DateTimeConverter
{
    /// <summary>
    /// Converts a Unix timestamp to a DateTime object.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp to convert.</param>
    /// <returns>A DateTime object representing the specified Unix timestamp.</returns>
    public static DateTime UnixTimestampToDateTime(long unixTimestamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime epoch = DateTime.UnixEpoch;
        return epoch.AddSeconds(unixTimestamp).ToLocalTime();
    }
}