namespace Cm.Api.Dtos;

// Small response shapes returned by the controllers. We deliberately do NOT serialize the EF
// entities directly: separate DTOs keep the API contract explicit and avoid navigation-property
// serialization cycles.

public record AssetSummaryDto(
    int Id, string Name, string Type, string Status, int SensorCount, int ActiveAlarmCount);

public record SensorDto(
    int Id, string Name, string Kind, string Unit,
    double? LowThreshold, double? HighThreshold, double? CurrentValue);

public record AssetDetailDto(
    int Id, string Name, string Type, string Status, IReadOnlyList<SensorDto> Sensors);

public record ReadingDto(DateTime Timestamp, double Value);

public record AlarmDto(
    int Id, int AssetId, int SensorId, string Type, string Message, string Severity,
    DateTime RaisedAt, bool Acknowledged, DateTime? AcknowledgedAt);

// Body for the manual ingestion endpoint. Timestamp is optional and defaults to "now".
public record IngestReadingRequest(double Value, DateTime? Timestamp);

public record ProposedActionDto(string Description, string Rationale, string Status);

public record HealthSummaryDto(string Status, ProposedActionDto ProposedAction);
