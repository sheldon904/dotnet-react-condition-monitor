import type { Alarm } from '../api';
import { acknowledgeAlarm } from '../api';

// Active alarms for the selected asset, each with an Acknowledge button. Acknowledging removes
// the alarm from the active list on the next poll (and the backend will not re-raise it unless
// the fault clears and recurs).
export function AlarmsPanel({ alarms, onAcknowledged }: { alarms: Alarm[]; onAcknowledged: () => void }) {
  const handleAck = async (id: number) => {
    await acknowledgeAlarm(id);
    onAcknowledged();
  };

  return (
    <div className="panel">
      <h3>Active Alarms</h3>
      {alarms.length === 0 && <div className="muted">No active alarms.</div>}
      {alarms.map((a) => (
        <div key={a.id} className={`alarm ${a.severity.toLowerCase()}`}>
          <div className="alarm-row">
            <span className={`sev ${a.severity.toLowerCase()}`}>{a.severity}</span>
            <span className="alarm-type">{a.type}</span>
            <span className="alarm-time">{new Date(a.raisedAt).toLocaleTimeString()}</span>
          </div>
          <div className="alarm-msg">{a.message}</div>
          <button className="ack-btn" onClick={() => handleAck(a.id)}>
            Acknowledge
          </button>
        </div>
      ))}
    </div>
  );
}
