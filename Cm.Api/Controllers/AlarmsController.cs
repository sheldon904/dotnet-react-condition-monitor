using Cm.Api.Common;
using Cm.Api.Data;
using Cm.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cm.Api.Controllers;

[ApiController]
[Route("api/alarms")]
public class AlarmsController : ControllerBase
{
    private readonly CmDbContext _db;

    public AlarmsController(CmDbContext db)
    {
        _db = db;
    }

    // GET /api/alarms?active=true : alarms, unacknowledged by default. active=false returns all.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlarmDto>>> GetAlarms([FromQuery] bool active = true)
    {
        var query = _db.Alarms.AsQueryable();
        if (active) query = query.Where(a => !a.Acknowledged);

        var alarms = await query.OrderByDescending(a => a.RaisedAt).ToListAsync();
        return alarms.Select(a => new AlarmDto(
            a.Id, a.AssetId, a.SensorId, a.Type, a.Message, a.Severity,
            TimeUtil.AsUtc(a.RaisedAt), a.Acknowledged, TimeUtil.AsUtc(a.AcknowledgedAt))).ToList();
    }

    // POST /api/alarms/{id}/acknowledge : mark acknowledged. 204 on success, 404 if missing.
    [HttpPost("{id:int}/acknowledge")]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var alarm = await _db.Alarms.FindAsync(id);
        if (alarm is null) return NotFound();

        alarm.Acknowledged = true;
        alarm.AcknowledgedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
