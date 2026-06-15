using Cm.Api.Faults;
using Cm.Api.Models;
using Xunit;

namespace Cm.Api.Tests;

// Unit tests for the most interesting piece of logic: the rule-based fault detector. It is
// pure (no database), so these run fast and need no setup.
public class RuleBasedFaultDetectorTests
{
    private readonly RuleBasedFaultDetector _detector = new();
    private static readonly IReadOnlySet<string> NoActiveAlarms = new HashSet<string>();

    private static Sensor Brush() => new()
    {
        Id = 1, AssetId = 1, Name = "Brush Length", Kind = "BrushLength", Unit = "%", LowThreshold = 30
    };

    private static Sensor Vibration() => new()
    {
        Id = 2, AssetId = 1, Name = "Vibration", Kind = "Vibration", Unit = "mm/s", HighThreshold = 7
    };

    private static IReadOnlyList<Reading> ReadingsOf(params double[] values)
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var list = new List<Reading>();
        for (int i = 0; i < values.Length; i++)
            list.Add(new Reading { SensorId = 1, Timestamp = start.AddSeconds(i), Value = values[i] });
        return list;
    }

    [Fact]
    public void LowBrushLength_BelowThreshold_RaisesCritical()
    {
        var faults = _detector.Detect(Brush(), ReadingsOf(95, 80, 50, 25), NoActiveAlarms);

        Assert.Contains(faults, f => f.Type == "Low Brush Length" && f.Severity == "Critical");
    }

    [Fact]
    public void BrushLength_AboveThreshold_RaisesNothing()
    {
        var faults = _detector.Detect(Brush(), ReadingsOf(95, 80, 50, 40), NoActiveAlarms);

        Assert.Empty(faults);
    }

    [Fact]
    public void HighVibration_AboveThreshold_RaisesWarning()
    {
        var faults = _detector.Detect(Vibration(), ReadingsOf(2, 2, 2, 2, 2, 2, 2, 2, 2, 8), NoActiveAlarms);

        Assert.Contains(faults, f => f.Type == "High Vibration" && f.Severity == "Warning");
    }

    [Fact]
    public void Vibration_AbnormalUpwardSpike_RaisesTrendBeforeHardThreshold()
    {
        // 29 flat readings then a value that is well above the recent mean but still below the
        // hard limit of 7. The z-score should flag the trend; the hard-threshold rule should not.
        var values = new List<double>();
        for (int i = 0; i < 29; i++) values.Add(2.0);
        values.Add(5.0);

        var faults = _detector.Detect(Vibration(), ReadingsOf(values.ToArray()), NoActiveAlarms);

        Assert.Contains(faults, f => f.Type == "Vibration Trend" && f.Severity == "Warning");
        Assert.DoesNotContain(faults, f => f.Type == "High Vibration");
    }

    [Fact]
    public void FlatData_RaisesNothing_AndDoesNotDivideByZero()
    {
        var values = Enumerable.Repeat(2.0, 30).ToArray();

        var faults = _detector.Detect(Vibration(), ReadingsOf(values), NoActiveAlarms);

        Assert.Empty(faults);
    }

    [Fact]
    public void Dedupe_SuppressesFaultWhenSameTypeAlreadyActive()
    {
        var existing = new HashSet<string> { "Low Brush Length" };

        var faults = _detector.Detect(Brush(), ReadingsOf(95, 80, 50, 25), existing);

        Assert.Empty(faults);
    }
}
