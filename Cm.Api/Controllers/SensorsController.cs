using Cm.Api.Common;
using Cm.Api.Data;
using Cm.Api.Dtos;
using Cm.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cm.Api.Controllers;

[ApiController]
[Route("api/sensors")]
public class SensorsController : ControllerBase
{
    private readonly CmDbContext _db;

    public SensorsController(CmDbContext db)
    {
        _db = db;
    }

    // GET /api/sensors/{id}/readings?minutes=10 : readings in the last N minutes for charting.
    [HttpGet("{id:int}/readings")]
    public async Task<ActionResult<IEnumerable<ReadingDto>>> GetReadings(int id, [FromQuery] int minutes = 10)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        var readings = await _db.Readings
            .Where(r => r.SensorId == id && r.Timestamp >= since)
            .OrderBy(r => r.Timestamp)
            .Select(r => new ReadingDto(r.Timestamp, r.Value))
            .ToListAsync();

        // SQLite drops DateTimeKind, so stamp the timestamps UTC for correct JSON.
        return readings.Select(r => r with { Timestamp = TimeUtil.AsUtc(r.Timestamp) }).ToList();
    }

    // POST /api/sensors/{id}/readings : ingest a single reading. Demonstrates the write path.
    [HttpPost("{id:int}/readings")]
    public async Task<ActionResult<ReadingDto>> IngestReading(int id, [FromBody] IngestReadingRequest body)
    {
        var sensor = await _db.Sensors.FindAsync(id);
        if (sensor is null) return NotFound();

        var reading = new Reading
        {
            SensorId = id,
            Value = body.Value,
            Timestamp = body.Timestamp ?? DateTime.UtcNow
        };
        _db.Readings.Add(reading);
        await _db.SaveChangesAsync();

        var dto = new ReadingDto(TimeUtil.AsUtc(reading.Timestamp), reading.Value);
        return Created($"/api/sensors/{id}/readings", dto);
    }
}
