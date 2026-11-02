<#
.SYNOPSIS
    Runs a SonarQube analysis of the Bonzer School ERP solution.

.DESCRIPTION
    begin -> build -> (tests with coverage, when test projects exist) -> end.
    Server URL and token come from parameters, environment variables or the repository's .env file
    (SONAR_HOST_URL, SONAR_TOKEN). Analysis settings: tools/sonar/SonarQube.Analysis.xml.

.EXAMPLE
    .\tools\sonar\sonar-scan.ps1
    .\tools\sonar\sonar-scan.ps1 -Token squ_xxx -HostUrl http://localhost:9000 -Version 1.2.0
#>
param(
    [string]$HostUrl,
    [string]$Token,
    [string]$ProjectKey = "bonzer-school-erp",
    [string]$ProjectName = "Bonzer School ERP",
    [string]$Version = "1.0.0"
)

# "Continue", not "Stop": Windows PowerShell 5.1 turns any stderr output of a native tool (e.g. the telemetry
# notice of dotnet-coverage) into a terminating error. Every tool's exit code is checked below instead.
$ErrorActionPreference = "Continue"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Push-Location $root
try {
    # Values from .env, unless given as parameters or environment variables.
    $envFile = Join-Path $root ".env"
    $dotenv = @{}
    if (Test-Path $envFile) {
        foreach ($line in Get-Content $envFile) {
            if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)\s*$') { $dotenv[$Matches[1]] = $Matches[2] }
        }
    }
    if (-not $HostUrl) { $HostUrl = $env:SONAR_HOST_URL }
    if (-not $HostUrl) { $HostUrl = $dotenv["SONAR_HOST_URL"] }
    if (-not $HostUrl) { $HostUrl = "http://localhost:9000" }
    if (-not $Token) { $Token = $env:SONAR_TOKEN }
    if (-not $Token) { $Token = $dotenv["SONAR_TOKEN"] }
    if (-not $Token) { throw "No SonarQube token. Pass -Token, or set SONAR_TOKEN in the environment or .env (see docs/sonarqube.md)." }

    $settings = Join-Path $root "tools\sonar\SonarQube.Analysis.xml"
    $testProjects = @(Get-ChildItem -Path $root -Recurse -Filter "*Tests.csproj" -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed" }

    dotnet sonarscanner begin /k:"$ProjectKey" /n:"$ProjectName" /v:"$Version" /s:"$settings" `
        /d:sonar.host.url="$HostUrl" /d:sonar.token="$Token"
    if ($LASTEXITCODE -ne 0) { throw "sonarscanner begin failed" }

    dotnet build HCB_API.sln --configuration Release --no-incremental
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }

    if ($testProjects.Count -gt 0) {
        dotnet dotnet-coverage collect "dotnet test HCB_API.sln --configuration Release --no-build" -f xml -o coverage.xml
        if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    }
    else {
        Write-Host "No *Tests.csproj found: analysing without coverage."
    }

    dotnet sonarscanner end /d:sonar.token="$Token"
    if ($LASTEXITCODE -ne 0) { throw "sonarscanner end failed" }

    Write-Host "Analysis uploaded: $HostUrl/dashboard?id=$ProjectKey"
}
finally {
    Pop-Location
}
