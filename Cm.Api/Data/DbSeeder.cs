using Cm.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cm.Api.Data;

// Creates the schema and seeds initial data on startup. No EF migrations are used; this is
// a demo, so EnsureCreated plus a one-time seed keeps the footprint small.
public static class DbSeeder
{
    private const int BackfillSeconds = 120;     // about two minutes of history
    private const double BrushBaseline = 100.0;  // healthy brush length (%)
    private const double VibrationBaseline = 2.0; // healthy vibration RMS (mm/s)

    public static void Seed(CmDbContext db)
    {
        db.Database.EnsureCreated();

        // Enable WAL so the one-per-second background writer and the API's polling reads do
        // not block each other ("database is locked"). This is a property of the file, set once.
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

        if (db.Assets.Any()) return; // already seeded on a previous run

        var asset = new Asset { Name = "Generator 1", Type = "Generator" };
        db.Assets.Add(asset);
        db.SaveChanges();

        var brush = new Sensor
        {
            AssetId = asset.Id, Name = "Brush Length", Kind = "BrushLength",
            Unit = "%", LowThreshold = 30
        };
        var vibration = new Sensor
        {
            AssetId = asset.Id, Name = "Vibration", Kind = "Vibration",
            Unit = "mm/s", HighThreshold = 7
        };
        db.Sensors.AddRange(brush, vibration);
        db.SaveChanges();

        // Backfill a HEALTHY baseline so the charts are populated on first load but no alarm
        // exists yet. The live decline and the vibration ramp begin at startup, so the
        // threshold crossings happen on screen during the demo rather than before it.
        var rng = new Random(1234); // fixed seed keeps the backfill repeatable
        var now = DateTime.UtcNow;
        var readings = new List<Reading>();
        for (int secondsAgo = BackfillSeconds; secondsAgo >= 1; secondsAgo--)
        {
            var t = now.AddSeconds(-secondsAgo);
            readings.Add(new Reading { SensorId = brush.Id, Timestamp = t, Value = BrushBaseline + Gaussian(rng) * 0.3 });
            readings.Add(new Reading { SensorId = vibration.Id, Timestamp = t, Value = VibrationBaseline + Gaussian(rng) * 0.1 });
        }
        db.Readings.AddRange(readings);
        db.SaveChanges();
    }

    // Standard normal sample (Box-Muller) used for gentle, realistic-looking noise.
    private static double Gaussian(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
