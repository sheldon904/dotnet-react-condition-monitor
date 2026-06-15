using Cm.Api.Models;

namespace Cm.Api.Faults;

// A classical, explainable fault detector. Every decision here is a simple, auditable rule
// with no hidden state. A trained ML model would plug in behind IFaultDetector instead; see
// the note on that interface for where the boundary sits.
public class RuleBasedFaultDetector : IFaultDetector
{
    // How many standard deviations above the recent mean counts as an abnormal upward trend.
    // A steadily rising series sits a little under 2 sigma above its own trailing mean, so the
    // threshold is set just below that to catch a developing climb while it is still building.
    private const double ZScoreThreshold = 1.8;
    // Require a little history before a z-score is meaningful.
    private const int MinReadingsForZScore = 10;
    // Only treat the z-score as meaningful once the window shows real variation. On a calm,
    // near-flat baseline the standard deviation is tiny, so a single noisy sample could produce
    // a misleadingly large z. Requiring a minimum spread means we only flag a trend once the
    // values have actually started moving, and it doubles as the divide-by-zero guard.
    private const double MinStdForTrend = 0.25;

    public IReadOnlyList<DetectedFault> Detect(
        Sensor sensor,
        IReadOnlyList<Reading> recentReadings,
        IReadOnlySet<string> existingActiveAlarmTypes)
    {
        var faults = new List<DetectedFault>();
        if (recentReadings.Count == 0) return faults;

        var values = recentReadings.Select(r => r.Value).ToList();
        var latest = values[^1];
        double? previous = values.Count >= 2 ? values[^2] : null;

        // Every rule below is EDGE-TRIGGERED: it raises a fault only as the condition first
        // becomes true (the previous reading was fine, this one is not), never on every tick a
        // sustained fault continues. That is what lets Acknowledge clear an alarm for good even
        // when the underlying condition persists: there is no fresh edge to re-raise it. The
        // dedupe at the end is the second line of defence for conditions that flicker.

        // Rule 1: brush length crossing below its low threshold is a hard, critical fault.
        if (sensor.Kind == "BrushLength" && sensor.LowThreshold is double low)
        {
            var nowLow = latest < low;
            var prevLow = previous is double p && p < low;
            if (nowLow && !prevLow)
            {
                faults.Add(new DetectedFault(
                    "Low Brush Length",
                    $"Brush length {latest:F1}{sensor.Unit} is below the {low:F0}{sensor.Unit} minimum.",
                    "Critical"));
            }
        }

        if (sensor.Kind == "Vibration")
        {
            // Rule 2: vibration crossing above its high threshold is a warning.
            if (sensor.HighThreshold is double high)
            {
                var nowHigh = latest > high;
                var prevHigh = previous is double p && p > high;
                if (nowHigh && !prevHigh)
                {
                    faults.Add(new DetectedFault(
                        "High Vibration",
                        $"Vibration {latest:F2} {sensor.Unit} exceeds the {high:F0} {sensor.Unit} limit.",
                        "Warning"));
                }
            }

            // Rule 3 (early warning): a rolling z-score flags an abnormal upward trend before
            // the hard threshold is hit. The z-score answers "how many standard deviations is
            // the latest value above the recent average?". We raise as the trend first turns
            // abnormal: the current window is over the threshold and the window one reading ago
            // was not.
            var zNow = ZScore(values);
            var zPrev = ZScore(values.Take(values.Count - 1).ToList());
            var trendIsNew = zNow is double zn && zn > ZScoreThreshold
                             && !(zPrev is double zp && zp > ZScoreThreshold);
            if (trendIsNew)
            {
                faults.Add(new DetectedFault(
                    "Vibration Trend",
                    $"Vibration is trending abnormally upward (z-score {zNow:F1}).",
                    "Warning"));
            }
        }

        // Dedupe: drop any fault whose Type already has an unacknowledged alarm for this
        // sensor. "High Vibration" and "Vibration Trend" are distinct Types, so both can be
        // active at once and the trend warning can appear before the hard threshold is crossed.
        return faults.Where(f => !existingActiveAlarmTypes.Contains(f.Type)).ToList();
    }

    // The z-score of the most recent value within the window, or null when there is too little
    // history or too little spread for it to mean anything.
    private static double? ZScore(IReadOnlyList<double> values)
    {
        if (values.Count < MinReadingsForZScore) return null;

        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        var std = Math.Sqrt(variance);
        if (std <= MinStdForTrend) return null;

        return (values[^1] - mean) / std;
    }
}
