namespace Cm.Api.Models;

// A measurement channel on an asset. Kind drives how the simulator generates values and
// how the fault detector interprets them. Thresholds are nullable because each sensor
// only uses the bound that makes sense for it (brush length has a low bound, vibration a
// high bound).
public class Sensor
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty; // "BrushLength" or "Vibration"
    public string Unit { get; set; } = string.Empty;
    public double? LowThreshold { get; set; }
    public double? HighThreshold { get; set; }
}
