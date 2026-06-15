import { useState } from 'react';
import { usePolling } from './hooks/usePolling';
import { getAssets, getAsset, getAlarms, getHealthSummary } from './api';
import { AssetList } from './components/AssetList';
import { SensorChart } from './components/SensorChart';
import { AlarmsPanel } from './components/AlarmsPanel';
import { HealthSummaryPanel } from './components/HealthSummaryPanel';
import { StatusBadge } from './components/StatusBadge';

// Single dashboard view. Everything polls every 2 seconds so charts and alarms update live.
export default function App() {
  const { data: assets } = usePolling(getAssets, 2000);

  // Track the selected asset; default to the first one until the user picks another.
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const effectiveId = selectedId ?? assets?.[0]?.id ?? null;

  // Bumped after an acknowledge so the alarms list refreshes immediately instead of waiting.
  const [ackNonce, setAckNonce] = useState(0);

  const { data: detail } = usePolling(
    () => (effectiveId != null ? getAsset(effectiveId) : Promise.resolve(null)),
    2000,
    [effectiveId],
  );
  const { data: alarms } = usePolling(() => getAlarms(true), 2000, [ackNonce]);
  const { data: health } = usePolling(
    () => (effectiveId != null ? getHealthSummary(effectiveId) : Promise.resolve(null)),
    2000,
    [effectiveId],
  );

  const assetAlarms = (alarms ?? []).filter((a) => (detail ? a.assetId === detail.id : false));

  return (
    <div className="app">
      <header className="app-header">
        <h1>Condition Monitor</h1>
        <span className="subtitle">single pane of glass &middot; live demo</span>
      </header>

      <div className="layout">
        <aside className="sidebar">
          <h3>Assets</h3>
          <AssetList assets={assets ?? []} selectedId={effectiveId} onSelect={setSelectedId} />
        </aside>

        <main className="content">
          {detail ? (
            <>
              <div className="detail-header">
                <h2>{detail.name}</h2>
                <StatusBadge status={detail.status} />
              </div>

              <div className="charts">
                {detail.sensors.map((s) => (
                  <SensorChart key={s.id} sensor={s} alarms={alarms ?? []} />
                ))}
              </div>

              <div className="panels">
                <AlarmsPanel alarms={assetAlarms} onAcknowledged={() => setAckNonce((n) => n + 1)} />
                <HealthSummaryPanel summary={health} />
              </div>
            </>
          ) : (
            <div className="muted">Loading...</div>
          )}
        </main>
      </div>
    </div>
  );
}
