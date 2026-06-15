namespace Cm.Api.Models;

// A fault raised by the detector. An alarm is "active" while Acknowledged is false.
// Severity is "Warning" or "Critical". Times are UTC.
public class Alarm
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public int SensorId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "Warning" or "Critical"
    public DateTime RaisedAt { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}
