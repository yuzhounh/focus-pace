param(
    [switch]$SkipPublish
)

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$publishScript = Join-Path $PSScriptRoot "publish.ps1"
$installerScript = Join-Path $repositoryRoot "installer\FocusPace.iss"
$appProject = Join-Path $repositoryRoot "src\FocusPace\FocusPace.csproj"
$publishedExecutable = Join-Path $repositoryRoot "artifacts\publish\win-x64\FocusPace.exe"
[xml]$appProjectXml = Get-Content -LiteralPath $appProject
$version = [string]($appProjectXml.Project.PropertyGroup.Version | Select-Object -First 1)
$portableExecutable = Join-Path $repositoryRoot "artifacts\installer\FocusPace-$version-win-x64-Portable.exe"
$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $compiler) {
    throw "Inno Setup 6 was not found. Install it with: winget install --id JRSoftware.InnoSetup --exact"
}

if (-not $SkipPublish) {
    & $publishScript -Runtime win-x64
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not (Test-Path -LiteralPath $publishedExecutable)) {
    throw "Published executable not found: $publishedExecutable"
}

& $compiler $installerScript
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item -LiteralPath $publishedExecutable -Destination $portableExecutable -Force

Write-Host "Built Focus Pace installer in artifacts\installer"
Write-Host "Built Focus Pace portable executable: $portableExecutable"
