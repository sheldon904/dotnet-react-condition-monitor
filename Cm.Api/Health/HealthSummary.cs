namespace Cm.Api.Health;

// A plain-language health read-out plus a single proposed action. The action defaults to
// "PendingApproval" and is never executed automatically; a human approves it first.
public record HealthSummary(string Status, ProposedAction ProposedAction);

public record ProposedAction(string Description, string Rationale, string Status = "PendingApproval");
