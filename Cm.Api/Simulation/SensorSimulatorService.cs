using Cm.Api.Data;
using Cm.Api.Faults;
using Cm.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cm.Api.Simulation;

// Generates simulated sensor data once per second, persists a reading per sensor, then runs
// the fault detector and persists any new alarms. This stands in for real data ingestion
// from field devices.
public class SensorSimulatorService : BackgroundService
{
    // --- Tunable simulation constants ---------------------------------------------------
    private const double BrushStart = 100.0;
    // Brush length declines a fixed amount each tick. 1.2 %/s takes it from 100 to 30 in
    // about 58 seconds, so the critical alarm fires roughly a minute after startup.
    private const double BrushDeclinePerTick = 1.2;

    private const int VibrationSampleCount = 256;        // synthetic waveform length per tick
    private const double VibrationStartAmplitude = 2.83; // gives a starting RMS near 2.0 mm/s
    private const double VibrationBaseGrowth = 0.03;     // base per-tick amplitude growth
    private const double VibrationGrowthAccel = 0.02;    // makes the growth accelerate over time

    private const int RecentWindow = 30; // readings handed to the detector (also the z-score window)
    // ------------------------------------------------------------------------------------

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFaultDetector _faultDetector;
    private readonly ILogger<SensorSimulatorService> _logger;

    private readonly Random _rng = new();
    private double _brushLength = BrushStart;
    private double _vibrationAmplitude = VibrationStartAmplitude;
    private double _phase;  // running phase of the sine waveform
    private int _tick;      // tick counter, drives the accelerating vibration growth

    public SensorSimulatorService(
        IServiceScopeFactory scopeFactory,
        IFaultDetector faultDetector,
        ILogger<SensorSimulatorService> logger)
    {
        _scopeFactory = scopeFactory;
        _faultDetector = faultDetector;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                Tick();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal on application shutdown.
        }
    }

    private void Tick()
    {
        // The hosted service is a singleton but CmDbContext is scoped (and not thread-safe),
        // so we open a fresh DI scope each tick and resolve the DbContext inside it. This is
        // the standard way to use EF Core from a long-lived background service.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CmDbContext>();

        var sensors = db.Sensors.ToList();
        var now = DateTime.UtcNow;
        _tick++;

        // 1) Generate and persist one reading per sensor.
        foreach (var sensor in sensors)
        {
            var value = sensor.Kind switch
            {
                "BrushLength" => NextBrushLength(),
                "Vibration" => NextVibrationRms(),
                _ => 0.0
            };
            db.Readings.Add(new Reading { SensorId = sensor.Id, Timestamp = now, Value = value });
        }
        db.SaveChanges();

        // 2) Run the fault detector per sensor and persist any new alarms.
        foreach (var sensor in sensors)
        {
            var recent = db.Readings
                .Where(r => r.SensorId == sensor.Id)
                .OrderByDescending(r => r.Timestamp)
                .Take(RecentWindow)
                .ToList();
            recent.Reverse(); // hand them to the detector oldest..newest

            var activeTypes = db.Alarms
                .Where(a => a.SensorId == sensor.Id && !a.Acknowledged)
                .Select(a => a.Type)
                .ToHashSet();

            foreach (var fault in _faultDetector.Detect(sensor, recent, activeTypes))
            {
                db.Alarms.Add(new Alarm
                {
                    AssetId = sensor.AssetId,
                    SensorId = sensor.Id,
                    Type = fault.Type,
                    Message = fault.Message,
                    Severity = fault.Severity,
                    RaisedAt = now,
                    Acknowledged = false
                });

                // Also write a clear line to the console for live demos.
                _logger.LogWarning("ALARM [{Severity}] {Type} on {Sensor}: {Message}",
                    fault.Severity, fault.Type, sensor.Name, fault.Message);
            }
        }
        db.SaveChanges();
    }

    private double NextBrushLength()
    {
        // Steady wear on the underlying value, with a little measurement noise on top so the
        // reported reading is not a perfectly straight line.
        _brushLength -= BrushDeclinePerTick;
        var reported = _brushLength + Gaussian() * 0.2;
        return Math.Max(0, reported);
    }

    private double NextVibrationRms()
    {
        // Grow the amplitude with an accelerating term, so the per-tick increase itself grows
        // over time and the RMS trend steepens. That accelerating shape is what lets the
        // rolling z-score flag an abnormal upward trend before the hard threshold is crossed.
        _vibrationAmplitude += VibrationBaseGrowth * (1.0 + VibrationGrowthAccel * _tick);

        // Build a short synthetic waveform (a sine plus gaussian noise) and reduce it to a
        // single trendable feature, the RMS. Deriving a feature like RMS from a raw waveform
        // mirrors how real condition-monitoring platforms turn raw signals into features to
        // trend and alarm on.
        double sumSquares = 0;
        for (int i = 0; i < VibrationSampleCount; i++)
        {
            _phase += 2 * Math.PI / 32.0; // about 32 samples per cycle
            var sample = _vibrationAmplitude * Math.Sin(_phase) + Gaussian() * 0.10;
            sumSquares += sample * sample;
        }
        return Math.Sqrt(sumSquares / VibrationSampleCount);
    }

    // Standard normal sample (Box-Muller).
    private double Gaussian()
    {
        var u1 = 1.0 - _rng.NextDouble();
        var u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
