using Cm.Api.Data;
using Cm.Api.Dtos;
using Cm.Api.Health;
using Cm.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cm.Api.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly CmDbContext _db;
    private readonly IHealthSummarizer _summarizer;

    public AssetsController(CmDbContext db, IHealthSummarizer summarizer)
    {
        _db = db;
        _summarizer = summarizer;
    }

    // GET /api/assets : every asset with its derived status, sensor count, and active-alarm count.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetSummaryDto>>> GetAssets()
    {
        var assets = await _db.Assets.ToListAsync();
        var result = new List<AssetSummaryDto>();
        foreach (var a in assets)
        {
            var sensorCount = await _db.Sensors.CountAsync(s => s.AssetId == a.Id);
            var active = await _db.Alarms.Where(al => al.AssetId == a.Id && !al.Acknowledged).ToListAsync();
            result.Add(new AssetSummaryDto(
                a.Id, a.Name, a.Type, AssetStatusCalculator.Derive(active), sensorCount, active.Count));
        }
        return result;
    }

    // GET /api/assets/{id} : asset detail with its sensors and the current value per sensor.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDetailDto>> GetAsset(int id)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset is null) return NotFound();

        var sensors = await _db.Sensors.Where(s => s.AssetId == id).ToListAsync();
        var active = await _db.Alarms.Where(al => al.AssetId == id && !al.Acknowledged).ToListAsync();

        var sensorDtos = new List<SensorDto>();
        foreach (var s in sensors)
        {
            var current = await _db.Readings
                .Where(r => r.SensorId == s.Id)
                .OrderByDescending(r => r.Timestamp)
                .Select(r => (double?)r.Value)
                .FirstOrDefaultAsync();
            sensorDtos.Add(new SensorDto(
                s.Id, s.Name, s.Kind, s.Unit, s.LowThreshold, s.HighThreshold, current));
        }

        return new AssetDetailDto(
            asset.Id, asset.Name, asset.Type, AssetStatusCalculator.Derive(active), sensorDtos);
    }

    // GET /api/assets/{id}/health-summary : grounded summary plus a single proposed action.
    [HttpGet("{id:int}/health-summary")]
    public async Task<ActionResult<HealthSummaryDto>> GetHealthSummary(int id)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset is null) return NotFound();

        var since = DateTime.UtcNow.AddMinutes(-10);
        var sensorIds = await _db.Sensors.Where(s => s.AssetId == id).Select(s => s.Id).ToListAsync();
        var recent = await _db.Readings
            .Where(r => sensorIds.Contains(r.SensorId) && r.Timestamp >= since)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();
        var active = await _db.Alarms.Where(al => al.AssetId == id && !al.Acknowledged).ToListAsync();

        var summary = await _summarizer.SummarizeAsync(asset, recent, active);
        return new HealthSummaryDto(
            summary.Status,
            new ProposedActionDto(
                summary.ProposedAction.Description,
                summary.ProposedAction.Rationale,
                summary.ProposedAction.Status));
    }
}
