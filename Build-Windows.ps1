param(
    [string]$Scene
)

$ErrorActionPreference = 'Stop'
$arguments = @(
    'command', 'build run',
    '--project-path', $PSScriptRoot,
    '--format', 'json',
    '--target', 'StandaloneWindows64',
    '--output-path', 'Builds/Windows/Game.exe',
    '--confirm', 'true'
)
if ($Scene) {
    $sceneJson = ConvertTo-Json -InputObject @($Scene) -Compress
    $arguments += @('--scenes', $sceneJson)
}
& unity @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Build queued. Check completion with: unity command "build status" --format json'
