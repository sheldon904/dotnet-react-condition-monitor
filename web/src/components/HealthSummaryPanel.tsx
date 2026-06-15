import { useEffect, useState } from 'react';
import type { HealthSummary } from '../api';

// Shows the grounded health summary plus the single proposed action. The proposal is clearly
// labeled as needing human approval, and the Approve button only flips the label: it never
// triggers any real action. This mirrors how a generated recommendation should sit behind a
// human gate rather than being executed automatically.
export function HealthSummaryPanel({ summary }: { summary: HealthSummary | null }) {
  const [approved, setApproved] = useState(false);

  // If a new proposal appears (its text changes), reset the approval state.
  useEffect(() => {
    setApproved(false);
  }, [summary?.proposedAction.description]);

  if (!summary) return null;

  const actionStatus = approved ? 'Approved' : summary.proposedAction.status;

  return (
    <div className="panel">
      <h3>Health Summary</h3>
      <p className="summary-text">{summary.status}</p>

      <div className="proposal">
        <div className="proposal-header">
          <span>Proposed action</span>
          <span className="approval-flag">Requires human approval</span>
        </div>
        <div className="proposal-desc">{summary.proposedAction.description}</div>
        <div className="proposal-rationale">{summary.proposedAction.rationale}</div>
        <div className="proposal-footer">
          <span className={`status-chip ${actionStatus === 'Approved' ? 'approved' : 'pending'}`}>
            {actionStatus}
          </span>
          <button className="approve-btn" disabled={approved} onClick={() => setApproved(true)}>
            {approved ? 'Approved' : 'Approve'}
          </button>
        </div>
      </div>

      <div className="muted small">
        This is a templated summary. Approval only flips the label in this demo and takes no real action.
      </div>
    </div>
  );
}
