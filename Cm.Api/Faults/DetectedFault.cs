namespace Cm.Api.Faults;

// A fault surfaced by the detector. It is a plain value: the caller (the simulation) turns
// it into a persisted Alarm row, setting the asset, sensor, and time. Keeping the detector
// free of those concerns is what makes it easy to unit test.
public record DetectedFault(string Type, string Message, string Severity);
