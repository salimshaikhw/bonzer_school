# SonarQube code quality

SonarQube checks the C# code for bugs, security vulnerabilities, code smells and duplication, and (once
test projects exist) test coverage. The same Sonar rules also run during the build of an analysis, as
`warning Sxxxx` messages.

## Files

| File | Purpose |
|---|---|
| `docker-compose.yml`, profile `sonar` | SonarQube Community Build on http://localhost:9000, with its own PostgreSQL (`sonar-db`). |
| `.config/dotnet-tools.json` | Pinned `dotnet-sonarscanner` and `dotnet-coverage` (installed by `dotnet tool restore`). |
| `tools/sonar/SonarQube.Analysis.xml` | Shared analysis settings: exclusions (migrations, generated code, legacy SQL), coverage report path. |
| `tools/sonar/sonar-scan.ps1` / `sonar-scan.sh` | One command: begin → build → tests with coverage (if any) → end. |
| `.github/workflows/sonarqube.yml` | Runs the analysis in GitHub Actions on push / pull request. |

## 1. Start SonarQube (once)

```bash
cp .env.example .env              # if not done yet
docker compose --profile sonar up -d sonarqube sonar-db
```

The first start takes 1–2 minutes. Open http://localhost:9000, log in with `admin` / `admin` and choose a new
password when asked.

Create a token: avatar (top right) → **My Account** → **Security** → *Generate Tokens*, type
**Global Analysis Token**. Put it in `.env`:

```
SONAR_TOKEN=sqa_...
```

## 2. Run an analysis

Windows (PowerShell):

```powershell
.\tools\sonar\sonar-scan.ps1
.\tools\sonar\sonar-scan.ps1 -Version 1.2.0          # label the analysis with a version
```

Linux / macOS / Git Bash:

```bash
tools/sonar/sonar-scan.sh
VERSION=1.2.0 tools/sonar/sonar-scan.sh
```

Requirements: .NET SDK 8+, Java 17+ (`java -version`). The results open at
http://localhost:9000/dashboard?id=bonzer-school-erp.

The project is created automatically on the first analysis with the default **Sonar way** quality gate.

## 3. In the editor (optional)

Install **SonarQube for IDE** (formerly SonarLint) for Visual Studio or VS Code and use *Connected Mode*
with `http://localhost:9000` and the project `bonzer-school-erp`: issues show while you type, with the
same rules and exclusions as the server.

## 4. GitHub Actions

`.github/workflows/sonarqube.yml` runs `tools/sonar/sonar-scan.sh` on pushes and pull requests to
`master`/`main`. Configure in the repository settings:

* secret `SONAR_TOKEN`
* variable `SONAR_HOST_URL` — a SonarQube server reachable from GitHub (not `localhost`)

Until both are set, the job is skipped instead of failing.

## Coverage

There are no test projects yet, so coverage shows 0 %. When a project named `*Tests.csproj` is added to
`HCB_API.sln`, the scripts run `dotnet test` under `dotnet-coverage` and upload `coverage.xml`
automatically. Entities, view models, migrations, mappings, seeders and `Program.cs` are excluded from
coverage (see `SonarQube.Analysis.xml`).

## Exclusions

Edit `tools/sonar/SonarQube.Analysis.xml`; it is used by both scripts and CI. Patterns are glob paths,
e.g. `**/Migrations/**`. To accept a single issue instead, mark it *Accepted* / *False positive* in the
SonarQube UI with a comment.

## Notes

* SonarQube Community Build analyses the main branch only; pull request decoration and branch analysis
  need a commercial edition or SonarQube Cloud.
* SonarQube data is kept in the `sonarqube-*` and `sonar-db-data` volumes. `docker compose --profile sonar down -v`
  deletes it.
