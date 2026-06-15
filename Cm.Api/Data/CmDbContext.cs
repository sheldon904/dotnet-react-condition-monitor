using Cm.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cm.Api.Data;

public class CmDbContext : DbContext
{
    public CmDbContext(DbContextOptions<CmDbContext> options) : base(options) { }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<Alarm> Alarms => Set<Alarm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Index the time-series table on (SensorId, Timestamp) so the common
        // "recent readings for this sensor" range query stays fast as Readings grows.
        modelBuilder.Entity<Reading>().HasIndex(r => new { r.SensorId, r.Timestamp });
    }
}
