#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SONAR_HOST="${SONAR_HOST:-http://localhost:9000}"
PROJECT_KEY="${PROJECT_KEY:-CaptionGen}"
PROJECT_NAME="${PROJECT_NAME:-CaptionGen}"

if [[ -z "${SONAR_TOKEN:-}" ]]; then
  echo "Error: SONAR_TOKEN is not set." >&2
  echo "Generate a token at $SONAR_HOST/account/security and run:" >&2
  echo "  SONAR_TOKEN=<your-token> ./scripts/sonar-scan.sh" >&2
  exit 1
fi

cd "$ROOT"

# ── 0. Clean stale coverage artifacts ────────────────────────────────────────
rm -rf "$ROOT/TestResults"

# ── 1. Python coverage ────────────────────────────────────────────────────────
echo ""
echo "==> [1/4] Running Python tests with coverage..."
cd "$ROOT/ai-service"
source .venv/bin/activate
pytest tests/ \
  --cov=. \
  --cov-config=setup.cfg \
  --cov-report=xml:coverage.xml \
  --cov-report=term-missing \
  -q || {
    echo "Warning: Python tests failed — coverage may be incomplete."
  }
deactivate
cd "$ROOT"

# ── 2. SonarScanner begin ─────────────────────────────────────────────────────
echo ""
echo "==> [2/4] Starting SonarScanner analysis..."
SONAR_ORG_ARGS=()
if [[ -n "${SONAR_ORGANIZATION:-}" ]]; then
  SONAR_ORG_ARGS=(/o:"$SONAR_ORGANIZATION")
fi

dotnet sonarscanner begin \
  /k:"$PROJECT_KEY" \
  "${SONAR_ORG_ARGS[@]}" \
  /n:"$PROJECT_NAME" \
  /d:sonar.host.url="$SONAR_HOST" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.projectBaseDir="$ROOT" \
  /d:sonar.exclusions="**/obj/**/*,**/bin/**/*,**/*.Designer.cs,**/node_modules/**/*,**/.venv/**/*,**/__pycache__/**/*,**/dist/**/*,**/TestResults/**/*,ai-service/tests/**/*,**/ai-service/tests/**/*,src/CaptionGen.Api/Contracts/**/*,**/src/CaptionGen.Api/Contracts/**/*,src/CaptionGen.Domain/**/*,**/src/CaptionGen.Domain/**/*,src/CaptionGen.Tests.Unit/**/*,**/src/CaptionGen.Tests.Unit/**/*,src/CaptionGen.Tests.Integration/**/*,**/src/CaptionGen.Tests.Integration/**/*" \
  /d:sonar.coverage.exclusions="**/Contracts/**/*,**/Migrations/**/*,**/Program.cs,src/CaptionGen.Domain/**/*,**/src/CaptionGen.Domain/**/*,src/frontend/**/*,**/src/frontend/**/*,ai-service/tests/**/*,**/ai-service/tests/**/*,src/CaptionGen.Tests.Unit/**/*,**/src/CaptionGen.Tests.Unit/**/*,src/CaptionGen.Tests.Integration/**/*,**/src/CaptionGen.Tests.Integration/**/*" \
  /d:sonar.cs.opencover.reportsPaths="$ROOT/TestResults/**/coverage.opencover.xml" \
  /d:sonar.python.coverage.reportPaths="$ROOT/ai-service/coverage.xml" \
  /d:sonar.python.version=3

# ── 3. Build + test .NET ──────────────────────────────────────────────────────
echo ""
echo "==> [3/4] Building and testing .NET solution..."
dotnet build CaptionGen.sln --no-incremental -c Release

dotnet test src/CaptionGen.Tests.Unit \
  --no-build \
  -c Release \
  --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/TestResults/unit" \
  --logger "trx;LogFileName=unit.trx"

# Integration tests require Docker (Testcontainers/PostgreSQL).
# Run them only when SONAR_RUN_INTEGRATION=1 is set.
if [[ "${SONAR_RUN_INTEGRATION:-0}" == "1" ]]; then
  echo ""
  echo "==> Running integration tests..."
  dotnet test src/CaptionGen.Tests.Integration \
    --no-build \
    -c Release \
    --settings coverlet.runsettings \
    --collect:"XPlat Code Coverage" \
    --results-directory "$ROOT/TestResults/integration" \
    --logger "trx;LogFileName=integration.trx"
fi

# ── 4. SonarScanner end ───────────────────────────────────────────────────────
echo ""
echo "==> [4/4] Sending results to SonarQube..."
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo ""
echo "Done. View results at: $SONAR_HOST/dashboard?id=$PROJECT_KEY"
