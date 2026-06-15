using Cm.Api.Models;

namespace Cm.Api.Health;

// Produces a grounded health summary for an asset.
//
// THIS IS THE LLM SEAM. A real implementation would call an LLM here, passing ONLY the
// grounded context (the recent readings and active alarms supplied below) into the prompt and
// returning a PROPOSED action for a human to approve. The default implementation is templated
// and deterministic so the repo runs offline with no API key.
public interface IHealthSummarizer
{
    Task<HealthSummary> SummarizeAsync(
        Asset asset,
        IReadOnlyList<Reading> recentReadings,
        IReadOnlyList<Alarm> activeAlarms);
}
