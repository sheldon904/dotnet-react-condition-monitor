namespace Cm.Api.Models;

// One time-series sample for a sensor. This is the table that grows over time as the
// simulation ticks. Timestamp is stored in UTC.
public class Reading
{
    public int Id { get; set; }
    public int SensorId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
