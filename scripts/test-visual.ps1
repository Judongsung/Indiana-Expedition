[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputDirectory,

    [switch]$SkipBuild,

    [switch]$SkipBaselineComparison
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$captureScript = Join-Path $PSScriptRoot "capture-wgc.ps1"
$baselineDirectory = Join-Path $repositoryRoot "tests\VisualBaselines"
$baselineManifestPath = Join-Path $baselineDirectory "manifest.json"
$captureToolPath = Join-Path `
    $repositoryRoot `
    "tools\IndianaExpedition.WgcCapture\bin\Release\net9.0-windows10.0.19041.0\IndianaExpedition.WgcCapture.exe"
$baselineManifest = Get-Content -LiteralPath $baselineManifestPath -Raw | ConvertFrom-Json
$visualHostPath = Join-Path `
    $repositoryRoot `
    "tests\IndianaExpedition.VisualTestHost\bin\x64\$Configuration\net48\IndianaExpedition.VisualTestHost.exe"
if (-not $SkipBuild) {
    $visualHostProject = Join-Path $repositoryRoot "tests\IndianaExpedition.VisualTestHost\IndianaExpedition.VisualTestHost.csproj"
    dotnet build $visualHostProject -c $Configuration -p:Platform=x64 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "VisualTestHost 빌드에 실패했습니다."
    }
}
if (-not (Test-Path -LiteralPath $visualHostPath)) {
    throw "VisualTestHost 실행 파일이 없습니다: $visualHostPath"
}
$visualStates = @(& $visualHostPath --list-visual-states --json | ConvertFrom-Json)
if ($LASTEXITCODE -ne 0 -or $visualStates.Count -eq 0) {
    throw "VisualTestHost에서 시각 상태 목록을 읽지 못했습니다."
}
if (-not $SkipBaselineComparison -and $baselineManifest.states.Count -ne $visualStates.Count) {
    throw "VisualTestHost와 기준선 manifest의 상태 수가 다릅니다."
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts\wgc"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$differenceDirectory = Join-Path $OutputDirectory "diff"
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
    if (-not $SkipBaselineComparison) {
        $baselineState = @($baselineManifest.states | Where-Object {
            [string]::Equals($_.state, $state, [StringComparison]::OrdinalIgnoreCase)
        })
        if ($baselineState.Count -ne 1) {
            throw "기준선 manifest에 상태가 없거나 중복됩니다: $state"
        }
        New-Item -ItemType Directory -Path $differenceDirectory -Force | Out-Null
        $baselinePath = Join-Path $baselineDirectory $baselineState[0].file
        if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) {
            throw "WGC 기준선 이미지가 없습니다: $baselinePath"
        }
        $baselineHash = (Get-FileHash -LiteralPath $baselinePath -Algorithm SHA256).Hash
        if (-not [string]::Equals(
                $baselineHash,
                $baselineState[0].sha256,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "WGC 기준선 이미지의 SHA-256이 manifest와 다릅니다: $state"
        }
        if ($result.Width -ne $baselineState[0].width -or
            $result.Height -ne $baselineState[0].height) {
            throw "WGC 캡처 크기가 manifest와 다릅니다: $state ($($result.Width)x$($result.Height))"
        }
        $differencePath = Join-Path $differenceDirectory ("indiana-expedition-{0}-diff.png" -f $state.ToLowerInvariant())
        $comparisonOutput = & $captureToolPath compare `
            --baseline $baselinePath `
            --actual $outputPath `
            --diff $differencePath `
            --channel-threshold $baselineManifest.thresholds.channelDifference `
            --max-changed-ratio $baselineManifest.thresholds.maximumChangedPixelRatio `
            --max-mean-error $baselineManifest.thresholds.maximumMeanAbsoluteRgbError 2>&1
        $comparisonExitCode = $LASTEXITCODE
        $comparisonText = ($comparisonOutput | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
        if ($comparisonExitCode -ne 0) {
            throw "WGC 기준선 비교 실패: $state`n$comparisonText`n차이 이미지: $differencePath"
        }
        $comparison = $comparisonText | ConvertFrom-Json
        $result | Add-Member -NotePropertyName BaselinePassed -NotePropertyValue $comparison.passed
        $result | Add-Member -NotePropertyName ChangedPixelRatio -NotePropertyValue $comparison.changedPixelRatio
        $result | Add-Member -NotePropertyName MeanAbsoluteRgbError -NotePropertyValue $comparison.meanAbsoluteRgbError
    }
    $results.Add($result)
}

$results | Select-Object State, CaptureMode, Width, Height, ForegroundUntouched, BaselinePassed, ChangedPixelRatio, MeanAbsoluteRgbError, Path
