using Cm.Api.Data;
using Cm.Api.Faults;
using Cm.Api.Health;
using Cm.Api.Simulation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Pin the API to a stable HTTP URL. HTTP (not HTTPS) in dev avoids the dev-cert trust
// prompt, and a fixed port gives the Vite app a known base URL to call.
builder.WebHost.UseUrls("http://localhost:5080");

// EF Core with SQLite. A single local file keeps the whole demo self-contained.
builder.Services.AddDbContext<CmDbContext>(options =>
    options.UseSqlite("Data Source=cm.db"));

// The fault detector is the "ML seam". It is pure, stateless logic, so a singleton is fine.
builder.Services.AddSingleton<IFaultDetector, RuleBasedFaultDetector>();

// The health summarizer is the "LLM seam". The templated default needs no API key.
builder.Services.AddSingleton<IHealthSummarizer, TemplatedHealthSummarizer>();

// The simulation runs as a background service (IHostedService) ticking once per second.
builder.Services.AddHostedService<SensorSimulatorService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string DevCors = "DevCors";
builder.Services.AddCors(options =>
{
    // Permissive CORS for local development so the Vite app (http://localhost:5173)
    // can call this API from a different origin.
    options.AddPolicy(DevCors, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Create the database and seed it (one asset, two sensors, and about two minutes of
// healthy backfill) once at startup, before the host starts handling requests.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CmDbContext>();
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(DevCors);
app.MapControllers();
app.Run();
