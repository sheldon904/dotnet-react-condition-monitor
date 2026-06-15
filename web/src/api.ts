// Typed client for the condition-monitoring API. The base URL can be overridden with a
// VITE_API_BASE env var; it defaults to the API's fixed dev port.
const BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5080/api';

export type AssetStatus = 'Healthy' | 'Warning' | 'Alarm';
export type Severity = 'Warning' | 'Critical';

export interface AssetSummary {
  id: number;
  name: string;
  type: string;
  status: AssetStatus;
  sensorCount: number;
  activeAlarmCount: number;
}

export interface SensorDetail {
  id: number;
  name: string;
  kind: string;
  unit: string;
  lowThreshold: number | null;
  highThreshold: number | null;
  currentValue: number | null;
}

export interface AssetDetail {
  id: number;
  name: string;
  type: string;
  status: AssetStatus;
  sensors: SensorDetail[];
}

export interface Reading {
  timestamp: string;
  value: number;
}

export interface Alarm {
  id: number;
  assetId: number;
  sensorId: number;
  type: string;
  message: string;
  severity: Severity;
  raisedAt: string;
  acknowledged: boolean;
  acknowledgedAt: string | null;
}

export interface HealthSummary {
  status: string;
  proposedAction: {
    description: string;
    rationale: string;
    status: string;
  };
}

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`);
  if (!res.ok) throw new Error(`GET ${path} failed: ${res.status}`);
  return (await res.json()) as T;
}

export const getAssets = () => getJson<AssetSummary[]>('/assets');
export const getAsset = (id: number) => getJson<AssetDetail>(`/assets/${id}`);
export const getReadings = (sensorId: number, minutes = 10) =>
  getJson<Reading[]>(`/sensors/${sensorId}/readings?minutes=${minutes}`);
export const getAlarms = (active = true) => getJson<Alarm[]>(`/alarms?active=${active}`);
export const getHealthSummary = (assetId: number) =>
  getJson<HealthSummary>(`/assets/${assetId}/health-summary`);

export async function acknowledgeAlarm(id: number): Promise<void> {
  const res = await fetch(`${BASE}/alarms/${id}/acknowledge`, { method: 'POST' });
  if (!res.ok) throw new Error(`acknowledge ${id} failed: ${res.status}`);
}
