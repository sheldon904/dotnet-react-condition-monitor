namespace Cm.Api.Common;

// SQLite does not preserve DateTimeKind, so values read back from the database come out as
// Unspecified. We mark them UTC at the serialization boundary so the JSON carries a 'Z'
// suffix and the browser parses the timestamps as UTC rather than local time.
public static class TimeUtil
{
    public static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? AsUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}
