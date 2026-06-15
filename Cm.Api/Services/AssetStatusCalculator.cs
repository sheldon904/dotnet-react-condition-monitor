using Cm.Api.Models;

namespace Cm.Api.Services;

// Asset health is DERIVED from active (unacknowledged) alarms, never stored. Any active
// Critical alarm means "Alarm", otherwise any active Warning means "Warning", otherwise
// "Healthy". Centralised here so the controllers and the health summarizer agree.
public static class AssetStatusCalculator
{
    public static string Derive(IEnumerable<Alarm> activeAlarms)
    {
        var list = activeAlarms.ToList();
        if (list.Any(a => a.Severity == "Critical")) return "Alarm";
        if (list.Any(a => a.Severity == "Warning")) return "Warning";
        return "Healthy";
    }
}
