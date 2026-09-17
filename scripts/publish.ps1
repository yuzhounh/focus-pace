param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent
)

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$dotnetCandidates = @(
    (Join-Path $repositoryRoot ".dotnet\dotnet.exe"),
    (Join-Path $repositoryRoot ".tools\dotnet\dotnet.exe"),
    "D:\Archives\20260914 Soft Trace\.tools\dotnet\dotnet.exe"
)
$dotnet = ($dotnetCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)
if (-not $dotnet) { $dotnet = "dotnet" }
$output = Join-Path $repositoryRoot "artifacts\publish\$Runtime"

$arguments = @(
    "publish",
    (Join-Path $repositoryRoot "src\FocusPace\FocusPace.csproj"),
    "--configuration", "Release",
    "--runtime", $Runtime,
    "--output", $output,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "--self-contained", (-not $FrameworkDependent).ToString().ToLowerInvariant()
)

& $dotnet @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Published FocusPace to $output"

