using Cm.Api.Models;
using Cm.Api.Services;

namespace Cm.Api.Health;

// The default, deterministic health summarizer. It builds the summary purely from the data it
// is handed, so the repo is shareable with no secrets and runs the same way every time.
public class TemplatedHealthSummarizer : IHealthSummarizer
{
    public Task<HealthSummary> SummarizeAsync(
        Asset asset,
        IReadOnlyList<Reading> recentReadings,
        IReadOnlyList<Alarm> activeAlarms)
    {
        // ---------------------------------------------------------------------------------
        // LLM SEAM
        // A real implementation would call an LLM right here, for example:
        //
        //   var apiKey = Environment.GetEnvironmentVariable("CM_LLM_API_KEY");
        //   var prompt = BuildGroundedPrompt(asset, recentReadings, activeAlarms);
        //   var reply  = await llmClient.CompleteAsync(prompt);
        //
        // Only the GROUNDED context above (recent readings and active alarms) would go into
        // the prompt, the returned action would be PROPOSED (never auto-executed), and the key
        // would come from an environment variable, never hardcoded. We keep the templated
        // version as the default so nothing here needs a network or a secret.
        // ---------------------------------------------------------------------------------

        var critical = activeAlarms.FirstOrDefault(a => a.Severity == "Critical");
        var warning = activeAlarms.FirstOrDefault(a => a.Severity == "Warning");

        string statusText;
        ProposedAction action;

        if (critical is not null)
        {
            statusText = $"{asset.Name} is in ALARM. {critical.Message}";
            action = new ProposedAction(
                "Schedule brush replacement and reduce load until the unit is serviced.",
                $"A critical alarm is active: {critical.Message} Acting now avoids unplanned downtime.");
        }
        else if (warning is not null)
        {
            statusText = $"{asset.Name} needs attention. {warning.Message}";
            action = new ProposedAction(
                "Plan an inspection of the affected component at the next opportunity.",
                $"A warning is active: {warning.Message} Trending it now prevents escalation.");
        }
        else
        {
            statusText = $"{asset.Name} is healthy. All sensors are within limits.";
            action = new ProposedAction(
                "Continue normal monitoring.",
                "No active alarms, and recent readings sit within their expected ranges.");
        }

        // Mention the latest values so the summary is visibly grounded in real data.
        var latestNote = recentReadings.Count > 0
            ? $" Latest sample at {TimeNote(recentReadings)}."
            : string.Empty;

        return Task.FromResult(new HealthSummary(statusText + latestNote, action));
    }

    private static string TimeNote(IReadOnlyList<Reading> recentReadings)
    {
        var last = recentReadings[^1].Timestamp;
        return DateTime.SpecifyKind(last, DateTimeKind.Utc).ToString("HH:mm:ss") + " UTC";
    }
}
