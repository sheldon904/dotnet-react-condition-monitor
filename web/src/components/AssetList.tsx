import type { AssetSummary } from '../api';
import { StatusBadge } from './StatusBadge';

// The asset overview: one selectable card per asset with its status badge and a short summary.
export function AssetList({
  assets,
  selectedId,
  onSelect,
}: {
  assets: AssetSummary[];
  selectedId: number | null;
  onSelect: (id: number) => void;
}) {
  if (assets.length === 0) return <div className="muted">No assets.</div>;

  return (
    <div className="asset-list">
      {assets.map((a) => (
        <button
          key={a.id}
          className={`asset-card ${a.id === selectedId ? 'selected' : ''}`}
          onClick={() => onSelect(a.id)}
        >
          <div className="asset-card-header">
            <span className="asset-name">{a.name}</span>
            <StatusBadge status={a.status} />
          </div>
          <div className="asset-meta">
            {a.sensorCount} sensors &middot; {a.activeAlarmCount} active{' '}
            {a.activeAlarmCount === 1 ? 'alarm' : 'alarms'}
          </div>
        </button>
      ))}
    </div>
  );
}
