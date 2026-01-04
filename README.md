# ImmoSearch

This project realized a Blazor Server app with a minimal API backend for scraping and listing real estate offers. The application is in active development and does not strive to be a general purpose solution. It is tailored to specific use cases and websites.

## Projects
- `src/ImmoSearch.Api` — minimal API, scraper orchestration
- `src/ImmoSearch.Web` — Blazor Server UI
- `src/ImmoSearch.Domain` / `Infrastructure` — core models, data, scraping
- `build/ImmoSearch.Build` — Nuke build automation

## Prerequisites
- .NET 10 SDK for build/run
- Docker (Desktop/CLI) for container workflows
- Visual Studio 2026 with "ASP.NET and web development" workload

## Run locally
- API: `dotnet run --project src/ImmoSearch.Api`
- Web: `dotnet run --project src/ImmoSearch.Web` (configure `ApiBaseUrl` if needed)

## Docker
- Build images: `./eng/build_docker_container.ps1`
- Relaunch (down/up, optional build/token/datadir):
  `./eng/relaunch_docker_container.ps1 [-Build] [-AdminToken "secret"] [-DataDir "C:\path\to\data"]`
- Compose files are in `eng/docker`; images `immosearch.api` / `immosearch.web`, project `immosearch`.
- SQLite is bind-mounted to `${DATA_DIR:-../../assets/docker}/data` so data persists across restarts.

## Nuke build
- Nuke (C# build automation) lives in `build/ImmoSearch.Build`. Scripts `build.ps1` / `build.sh` in the repo root run without extra setup on Windows/Linux/macOS.
- Restore+build: `./build.ps1 --target Compile` (or `./build.sh --target Compile`)
- Docker relaunch via Nuke: `./build.ps1 --target DockerRelaunch --DockerBuild --DockerAdminToken secret --DockerDataDir "C:\data\immo"`
- Nuke calls the PowerShell scripts in `eng/`; you can also run them directly (see Docker section).

## Admin
- Admin endpoints/actions require `Admin:Token` if configured; set via env `Admin__Token` (e.g., in compose or scripts).
- Settings are readable without token; edits/scrape/reset need the token. Admin UI at `/admin`.

## Scraping
- Uses stored scrape settings; skips runs if none exist.
- Interval default via `Scraping:DefaultIntervalSeconds`, optional override per settings (`IntervalSeconds`).

## Dev notes
- Currency/culture defaults to `de-AT` for Euro formatting.
- OpenAPI/Scalar UI is always enabled on the API.

## License
This project is licensed under the GNU GPL v3.0. See `LICENSE` for details.
