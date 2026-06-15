# web

React + Vite + TypeScript dashboard for the Mini Condition-Monitoring App.

It polls the .NET API and shows live trend charts, thresholds, and acknowledgeable alarms.
See the [repository README](../README.md) for the full picture and run instructions.

## Quick start

```
npm install
npm run dev
```

Then open `http://localhost:5173`. The API must be running at `http://localhost:5080`
(override with `VITE_API_BASE` if needed). Start the API with `dotnet run` in `/Cm.Api`.
