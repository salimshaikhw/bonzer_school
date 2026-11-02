#!/usr/bin/env bash
# Runs a SonarQube analysis of the Bonzer School ERP solution (Linux / macOS / CI).
#   SONAR_TOKEN=squ_xxx tools/sonar/sonar-scan.sh
#   SONAR_HOST_URL=https://sonar.example.com SONAR_TOKEN=... VERSION=1.2.0 tools/sonar/sonar-scan.sh
# Settings: tools/sonar/SonarQube.Analysis.xml. Values missing from the environment are read from .env.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$root"

dotenv() { [ -f .env ] && grep -E "^$1=" .env | tail -n 1 | cut -d= -f2- || true; }

SONAR_HOST_URL="${SONAR_HOST_URL:-$(dotenv SONAR_HOST_URL)}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:9000}"
SONAR_TOKEN="${SONAR_TOKEN:-$(dotenv SONAR_TOKEN)}"
PROJECT_KEY="${PROJECT_KEY:-bonzer-school-erp}"
PROJECT_NAME="${PROJECT_NAME:-Bonzer School ERP}"
VERSION="${VERSION:-1.0.0}"

if [ -z "$SONAR_TOKEN" ]; then
  echo "No SonarQube token: set SONAR_TOKEN in the environment or .env (see docs/sonarqube.md)." >&2
  exit 1
fi

dotnet tool restore

dotnet sonarscanner begin /k:"$PROJECT_KEY" /n:"$PROJECT_NAME" /v:"$VERSION" \
  /s:"$root/tools/sonar/SonarQube.Analysis.xml" \
  /d:sonar.host.url="$SONAR_HOST_URL" /d:sonar.token="$SONAR_TOKEN"

dotnet build HCB_API.sln --configuration Release --no-incremental

if find . -name '*Tests.csproj' -not -path '*/bin/*' -not -path '*/obj/*' | grep -q .; then
  dotnet dotnet-coverage collect "dotnet test HCB_API.sln --configuration Release --no-build" -f xml -o coverage.xml
else
  echo "No *Tests.csproj found: analysing without coverage."
fi

dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo "Analysis uploaded: $SONAR_HOST_URL/dashboard?id=$PROJECT_KEY"
