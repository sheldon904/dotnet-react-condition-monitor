namespace Cm.Api.Models;

// A monitored piece of equipment, for example a generator. Its health Status is derived
// at read time from active alarms, never stored on the row.
public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
