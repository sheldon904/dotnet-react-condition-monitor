using Cm.Api.Models;

namespace Cm.Api.Faults;

// The boundary between raw data and fault decisions.
//
// THIS IS THE ML SEAM. In production a trained fault model would sit behind this interface.
// The heavy model would typically run as a separate Python service called over HTTP; this
// interface is the boundary on the .NET side. The shipped implementation is classical and
// fully explainable rules, not a faked model.
public interface IFaultDetector
{
    // Inspect a sensor and its recent readings (oldest..newest) and return zero or more
    // faults. existingActiveAlarmTypes carries the Types of this sensor's currently
    // unacknowledged alarms so the detector can suppress duplicates.
    IReadOnlyList<DetectedFault> Detect(
        Sensor sensor,
        IReadOnlyList<Reading> recentReadings,
        IReadOnlySet<string> existingActiveAlarmTypes);
}
