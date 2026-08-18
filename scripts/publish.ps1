param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent
)

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $repositoryRoot ".dotnet\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { "dotnet" }
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

