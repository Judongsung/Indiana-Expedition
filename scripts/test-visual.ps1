[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputDirectory,

    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$captureScript = Join-Path $PSScriptRoot "capture-wgc.ps1"
$visualStates = @("Main", "Favorites", "History")

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts\wgc"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$results = New-Object 'System.Collections.Generic.List[object]'
for ($index = 0; $index -lt $visualStates.Count; $index++) {
    $state = $visualStates[$index]
    $outputPath = Join-Path $OutputDirectory ("indiana-expedition-{0}.png" -f $state.ToLowerInvariant())
    $captureArguments = @{
        Configuration = $Configuration
        State = $state
        OutputPath = $outputPath
    }

    if ($SkipBuild -or $index -gt 0) {
        $captureArguments.SkipBuild = $true
    }

    $result = & $captureScript @captureArguments
    $results.Add($result)
}

$results | Select-Object State, CaptureMode, Width, Height, ForegroundUntouched, Path
