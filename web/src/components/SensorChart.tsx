import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
  ReferenceDot,
} from 'recharts';
import type { SensorDetail, Alarm } from '../api';
import { getReadings } from '../api';
import { usePolling } from '../hooks/usePolling';

function formatTime(ms: number): string {
  return new Date(ms).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

// One sensor's trend over the last 10 minutes: the line, a dashed threshold ReferenceLine, and
// a dot marking where each alarm for this sensor was raised. Polls its own readings every 2s.
export function SensorChart({ sensor, alarms }: { sensor: SensorDetail; alarms: Alarm[] }) {
  const { data: readings } = usePolling(() => getReadings(sensor.id, 10), 2000, [sensor.id]);

  // recharts plots best against numeric x values, so map timestamps to epoch milliseconds.
  const points = (readings ?? []).map((r) => ({ t: Date.parse(r.timestamp), v: r.value }));

  // Brush length is bad when it falls below its low bound; vibration when it rises above its
  // high bound. Show whichever threshold applies to this sensor.
  const threshold = sensor.kind === 'BrushLength' ? sensor.lowThreshold : sensor.highThreshold;

  // Place a marker on the line at each alarm's time by snapping it to the nearest reading.
  // flatMap lets us drop alarms that have no readings to anchor to without null juggling.
  const marks = alarms
    .filter((a) => a.sensorId === sensor.id)
    .flatMap((a) => {
      if (points.length === 0) return [];
      const at = Date.parse(a.raisedAt);
      let nearest = points[0];
      for (const p of points) {
        if (Math.abs(p.t - at) < Math.abs(nearest.t - at)) nearest = p;
      }
      return [{ t: nearest.t, v: nearest.v, severity: a.severity }];
    });

  return (
    <div className="chart-card">
      <div className="chart-title">
        {sensor.name} <span className="chart-unit">({sensor.unit})</span>
        {sensor.currentValue != null && (
          <span className="chart-current">now {sensor.currentValue.toFixed(1)}</span>
        )}
      </div>
      <ResponsiveContainer width="100%" height={220}>
        <LineChart data={points} margin={{ top: 10, right: 24, bottom: 4, left: -8 }}>
          <CartesianGrid stroke="#262b36" />
          <XAxis
            dataKey="t"
            type="number"
            domain={['dataMin', 'dataMax']}
            tickFormatter={formatTime}
            stroke="#7c8497"
            minTickGap={48}
            tick={{ fontSize: 11 }}
          />
          <YAxis stroke="#7c8497" domain={['auto', 'auto']} tick={{ fontSize: 11 }} />
          <Tooltip
            labelFormatter={(l) => formatTime(l as number)}
            formatter={(value) => [Number(value).toFixed(2), sensor.unit]}
            contentStyle={{ background: '#11151c', border: '1px solid #262b36', borderRadius: 6 }}
          />
          <Line
            type="monotone"
            dataKey="v"
            stroke="#4f9cf9"
            strokeWidth={2}
            dot={false}
            isAnimationActive={false}
          />
          {threshold != null && (
            <ReferenceLine
              y={threshold}
              stroke="#d2453f"
              strokeDasharray="5 4"
              ifOverflow="extendDomain"
              label={{
                value: `threshold ${threshold}`,
                fill: '#e0736e',
                position: 'insideTopRight',
                fontSize: 11,
              }}
            />
          )}
          {marks.map((m, i) => (
            <ReferenceDot
              key={i}
              x={m.t}
              y={m.v}
              r={5}
              fill={m.severity === 'Critical' ? '#d2453f' : '#e0a526'}
              stroke="#fff"
              strokeWidth={1}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
