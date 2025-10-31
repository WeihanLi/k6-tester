# k6-tester

`k6-tester` is a minimal ASP.NET Core application that helps you design, run, and share [k6](https://k6.io) load tests through a web UI. The single-page interface lets you configure scenarios, headers, payloads, thresholds, and tags, generate the matching JavaScript script, and execute it directly on the machine where the web app is running while streaming the k6 output back to the browser.

## Features

- **Visual script builder** – fill out form fields for constant or ramping VU profiles, HTTP settings, headers, payloads, thresholds, and tags to generate a ready-to-run script.
- **Instant script download** – copy, download, or view the `k6 run` command for the generated script.
- **One-click execution** – run the script via the local k6 CLI and monitor live stdout/stderr output in the UI (useful for demos or quick validation runs).
- **REST API** – `/api/k6/script` returns JSON containing the generated JS script; `/api/k6/run` streams real-time execution output (text/plain).
- **Unit tests** – xUnit specs cover core script generation behaviors.

## Project layout

```
.
├── README.md                # Project overview (this file)
├── k6-tester.sln            # Solution file referencing src/ and test/ projects
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

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [k6 CLI](https://k6.io/docs/get-started/installation/) available on the machine/path where you run the app (optional but required for the **Run with k6** button to work).

## Getting started

```bash
# Restore dependencies and build
dotnet build

# Run the web app
dotnet run --project src/k6-tester/k6-tester.csproj
```

Then open your browser at `http://localhost:5000` (or the URL printed by `dotnet run`). Adjust configuration parameters in the UI, click **Generate script** to preview, and **Run with k6** to execute the script locally.

## Running tests

```bash
dotnet test test/k6-tester.Tests/k6-tester.Tests.csproj
```

## Next steps

- Deploy behind authentication if exposing beyond local environments—production use should restrict who can run arbitrary k6 scripts.
- Extend the runner service to allow uploading existing scripts or persisting generated ones for reuse.
