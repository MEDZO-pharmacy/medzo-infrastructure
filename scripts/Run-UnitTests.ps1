[CmdletBinding()]
param(
    [switch]$SkipFrontend
)

$ErrorActionPreference = 'Stop'
$infrastructureRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $infrastructureRoot
$resultsRoot = Join-Path $infrastructureRoot 'test-results/unit-coverage'

New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null

function Invoke-TestProject {
    param([string]$Name, [string]$ProjectPath)

    if (-not (Test-Path $ProjectPath)) {
        throw "$Name test project was not found: $ProjectPath"
    }

    $projectResults = Join-Path $resultsRoot ($Name -replace '[^a-zA-Z0-9-]', '-')
    New-Item -ItemType Directory -Force -Path $projectResults | Out-Null
    Write-Host "`n==> $Name" -ForegroundColor Cyan
    dotnet test $ProjectPath --collect:"XPlat Code Coverage" --results-directory $projectResults
    if ($LASTEXITCODE -ne 0) { throw "$Name failed." }
}

Invoke-TestProject 'Auth' (Join-Path $workspaceRoot 'medzo-user-auth-service/tests/Medzo.Auth.UnitTests/Medzo.Auth.UnitTests.csproj')
Invoke-TestProject 'CatalogueInventory' (Join-Path $workspaceRoot 'medzo-catalogue-inventory-service/tests/Medzo.CatalogueInventory.UnitTests/Medzo.CatalogueInventory.UnitTests.csproj')
Invoke-TestProject 'PurchasingSupplier' (Join-Path $workspaceRoot 'medzo-purchasing-supplier-service/tests/Medzo.PurchasingSupplier.UnitTests/Medzo.PurchasingSupplier.UnitTests.csproj')
Invoke-TestProject 'SalesReporting' (Join-Path $workspaceRoot 'medzo-sales-reporting-service/tests/Medzo.SalesReporting.Tests/Medzo.SalesReporting.Tests.csproj')

if (-not $SkipFrontend) {
    $frontendPath = Join-Path $workspaceRoot 'medzo-frontend'
    if (-not (Test-Path (Join-Path $frontendPath 'package.json'))) { throw "Frontend package was not found: $frontendPath" }
    Write-Host "`n==> Frontend" -ForegroundColor Cyan
    Push-Location $frontendPath
    try {
        npm run test
        if ($LASTEXITCODE -ne 0) { throw 'Frontend tests failed.' }
    }
    finally { Pop-Location }
}

Write-Host "`nAll unit-test results are in $resultsRoot" -ForegroundColor Green
