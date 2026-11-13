param(
    [switch]$SelfContained = $true,
    [string]$OutputDir = "publish\RungChuongVang_Win64"
)

$ErrorActionPreference = "Stop"

Write-Host "=== DONG GOI PHAN MEM RUNG CHUONG VANG ===" -ForegroundColor Cyan

$scFlag = if ($SelfContained) { "true" } else { "false" }

dotnet restore "RungChuongVang.csproj"
dotnet build "RungChuongVang.csproj" -c Release -p:Platform=x64 --no-restore

dotnet publish "RungChuongVang.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained $scFlag `
    -p:PlatformTarget=x64 `
    -p:PublishReadyToRun=$scFlag `
    -o $OutputDir

$dataDirs = @(
    "Data\Images\Questions",
    "Data\Images\Winners",
    "Data\Images\Teams",
    "Data\Images\Contestants",
    "Data\Images\Logo",
    "Data\Backup",
    "Data\Exports",
    "Data\Logs"
)

foreach ($dir in $dataDirs) {
    $full = Join-Path $OutputDir $dir
    if (-not (Test-Path $full)) {
        New-Item -ItemType Directory -Path $full -Force | Out-Null
    }
}

if (Test-Path "Docs") {
    Copy-Item -Path "Docs" -Destination (Join-Path $OutputDir "Docs") -Recurse -Force
}
if (Test-Path "README.md") {
    Copy-Item -Path "README.md" -Destination (Join-Path $OutputDir "README.md") -Force
}

Write-Host "Hoan tat! Goi phat hanh nam tai: $OutputDir" -ForegroundColor Green
