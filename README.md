# k6-tester

`k6-tester` is a minimal ASP.NET Core application that helps you design, run, and share [k6](https://k6.io) load tests through a web UI. The single-page interface lets you configure scenarios, headers, payloads, thresholds, and tags, generate the matching JavaScript script, and execute it directly on the machine where the web app is running while streaming the k6 output back to the browser.

![k6 script builder](./assets/builder.png)

![k6 script editor/runner](./assets/script.png)

## Features

- **Visual script builder** – fill out form fields for constant or ramping VU profiles, HTTP settings, headers, payloads, thresholds, and tags to generate a ready-to-run script.
- **Instant script download** – copy, download, or view the `k6 run` command for the generated script.
- **One-click execution** – run the script via the local k6 CLI and monitor live stdout/stderr output in the UI (useful for demos or quick validation runs).
- **REST API** – `/api/k6/script` returns JSON containing the generated JS script; `/api/k6/run` streams real-time execution output (text/plain).

## Project layout

```
.
├── README.md                # Project overview (this file)
├── k6-tester.slnx           # Solution file referencing src/ and test/ projects
├── src/
│   └── k6-tester/           # ASP.NET Core minimal API + static SPA
│       ├── Models/          # DTOs for script configuration & responses
│       ├── Services/        # Script builder and k6 runner helpers
│       ├── wwwroot/         # HTML/JS single-page UI
│       └── Program.cs       # Minimal API setup
└── test/
    └── k6-tester.Tests/     # xUnit tests for script builder logic
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [k6 CLI](https://k6.io/docs/get-started/installation/) available on the machine/path where you run the app (optional but required for the **Run with k6** button to work).

## Getting started

```bash
# Restore dependencies and build
dotnet build

# Run the web app
dotnet run --project src/k6-tester/k6-tester.csproj
```

Then open your browser at `http://localhost:5266/` (or the URL printed by `dotnet run`). Adjust configuration parameters in the UI, click **Generate script** to preview, and **Run with k6** to execute the script locally.

## Running tests

```bash
dotnet test test/k6-tester.Tests/k6-tester.Tests.csproj
```

## Running in Docker

The repository ships with a multi-stage `Dockerfile` that compiles the app, installs the k6 CLI in the final image, and exposes the web UI on port 8080.

```bash
# Build the container image
docker build -t k6-tester .

# Run it
docker run --rm -p 8080:8080 k6-tester
```

or run prebuilt image directly

```bash
docker run --rm -p 8080:8080 weihanli/k6-tester
```

Then browse to `http://localhost:8080`. Any UI-triggered k6 runs execute inside the container thanks to the bundled k6 binary.

## Roadmap

1. **Scenario library & persistence** – allow saving generated configs/scripts to a backing store so common tests can be versioned, shared, and rerun with minimal tweaks.
2. **Auth-enabled deployments** – integrate with ASP.NET Core authentication/authorization to gate who can generate or execute load tests in shared environments.
3. **Advanced runners** – add pluggable back ends for remote execution (e.g., k6 cloud, distributed agents, or containerized workers) with run status dashboards.
4. **Observability hooks** – stream metrics to Prometheus/InfluxDB/Grafana for richer reporting and optionally expose summarized charts in the UI.
5. **Extensible script templates** – support custom JS snippets, checks, and thresholds so teams can extend the base generator with reusable building blocks.

## Next steps

- Deploy behind authentication if exposing beyond local environments—production use should restrict who can run arbitrary k6 scripts.
- Extend the runner service to allow uploading existing scripts or persisting generated ones for reuse.
