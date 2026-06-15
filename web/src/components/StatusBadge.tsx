// A small color-coded status pill. Green Healthy, amber Warning, red Alarm.
const COLORS: Record<string, string> = {
  Healthy: '#2e9e5b',
  Warning: '#e0a526',
  Alarm: '#d2453f',
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <span className="badge" style={{ backgroundColor: COLORS[status] ?? '#5a6172' }}>
      {status}
    </span>
  );
}
