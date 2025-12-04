<!-- markdownlint-configure-file {"MD013": {"line_length": 420}} -->

# Agent Playbook

This document is for automated coding agents that need precise, repeatable instructions while working in this repository. Follow these guidelines to stay aligned with the human-facing docs and CI workflows.

## TL;DR workflow

1. Restore once per session: `dotnet restore k6-tester.slnx`.
2. Build Release (CI parity): `dotnet build k6-tester.slnx --configuration Release --no-restore`.
3. Test Release (no build): `dotnet test test/k6-tester.Tests/k6-tester.Tests.csproj --configuration Release --no-build`.
4. Launch the app when you need manual verification: `dotnet run --project src/k6-tester/k6-tester.csproj`.
5. Before exercising the **Run with k6** button or the `/api/k6/run` endpoint locally, run `k6 version` to ensure the CLI is on `PATH`; missing binaries intentionally surface as streamed `[error]` messages.

## Quick Orientation

- Application code lives in `src/k6-tester/` (ASP.NET Core minimal API + static SPA assets under `wwwroot/`).
- Tests live in `test/k6-tester.Tests/` and use xUnit v3.
- The solution manifest is `k6-tester.slnx`; there is no traditional `.sln`.
- Docker support is provided via the root `Dockerfile`, which produces an image that already contains the k6 CLI.
- Health probes exist at `/health/live`, while `/api/k6/script` and `/api/k6/run` provide the JSON script builder and streaming runner that the SPA consumes.

## Toolchain & Build Steps

- Target framework: `net10.0`. Do not downgrade framework or SDK unless explicitly directed; keep project and test targets in sync.
- Restore once per session:\
  `dotnet restore k6-tester.slnx`
- Build the entire solution the same way CI does:\
  `dotnet build k6-tester.slnx --configuration Release --no-restore`
- Release configuration builds are the default expectation. If you build Debug locally, follow up with a Release build before validating changes.

## Running Tests

- Primary test command (mirrors CI):\
  `dotnet test test/k6-tester.Tests/k6-tester.Tests.csproj --configuration Release --no-build`
- Tests rely only on deterministic string output from `K6ScriptBuilder`; they do not execute the k6 binary. Continue that pattern—avoid spawning external processes in unit tests.
- Coverage is optional but available via coverlet:\
  `dotnet test ... /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`

## Local Execution

- Run the web app with:\
  `dotnet run --project src/k6-tester/k6-tester.csproj`
- The minimal API uses `MapFallbackToFile("index.html")` and serves the SPA from `wwwroot/`. No additional build or bundling step is required.
- Runtime k6 execution requires the `k6` CLI on `PATH`. If it is missing, the API responds with a streamed error message—this is by design, so do not suppress it.
- A liveness endpoint is available at `/health/live`; keep it lightweight so Docker/AKS probes remain fast.

## Docker Workflow

- Build the multi-arch image locally:\
  `docker build -t k6-tester .`
- The `K6_VERSION` build argument controls the bundled CLI version. Update both the argument default and any CI references together if you bump it.
- GitHub Actions publishes images using `docker/build-push-action` for `linux/amd64` and `linux/arm64`; verify any Dockerfile changes against that matrix.

## Coding Conventions

- Prefer file-scoped namespaces (`namespace K6Tester.Services;`) and keep using static helper classes where they exist (e.g., `K6ScriptBuilder`, `K6Runner`).
- Maintain nullable reference type annotations and avoid introducing unnecessary `!` suppression.
- Follow existing guard clause patterns (`ArgumentNullException.ThrowIfNull`) and streaming patterns in `K6Runner`; changes here must preserve cancellation and cleanup semantics.
- Keep string-building logic in `K6ScriptBuilder` straightforward—tests assert against specific substrings, so favor incremental additions over wholesale rewrites.
- Static assets in `wwwroot/index.html` are hand-authored. Avoid reformatting or regenerating the file unless intentionally updating the UI; large diffs make reviews harder.
- The SPA stores small amounts of state in `localStorage` (layout preference, last script). Preserve that behavior when making UI changes so that share-link flows keep working.

## Adding or Updating Tests

- Use xUnit v3 attributes (`[Fact]`, `[Theory]`) and keep new tests inside `K6ScriptBuilderTests.cs` unless factoring out new surface area.
- When verifying generated scripts, assert on targeted substrings instead of the entire script to keep tests resilient to formatting tweaks.
- If you introduce new configuration fields, add serialization coverage in both `K6ScriptBuilder` and the corresponding tests.

## CI Expectations

- The `ci.yml` workflow checks out the repo, runs `dotnet restore`, builds Release, and executes `dotnet test` (Release, no build). Ensure the commands above succeed locally before opening a PR.
- The `docker.yml` workflow depends on successful builds and expects Dockerfiles to remain multi-arch friendly. Test Buildx changes locally when possible.
- The `markdown-lint.yml` workflow checks the markdown format to ensure we use the same style across the repository.

Following this playbook keeps automated and human contributors aligned while preventing CI surprises.
