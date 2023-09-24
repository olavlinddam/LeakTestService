using System.Text.RegularExpressions;

namespace LeakTestService.Models;

public class TimeRange
{
    /// <summary>
    /// This class is used to represent a time range that the client specifies. The range i used to limit the timerange
    /// that we query within.
    /// </summary>
    public DateTime Start { get; set; }
    public DateTime? Stop { get; set; }

    /// <summary>
    /// Takes a start date and an optional end date and converts the values to UTC.
    /// </summary>
    /// <param name="start">Required, since InfluxDB requires a start value for the time range.</param>
    /// <param name="stop">Optional. If not provided the time range will be from the provided start value and until
    /// the most recent entry in the database.</param>
    public TimeRange(DateTime start, DateTime? stop)
    {
        Start = start.ToUniversalTime();

        if (stop != null)
        {
            Stop = stop.Value.ToUniversalTime();
        }
    }
}