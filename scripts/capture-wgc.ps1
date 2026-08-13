[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet(
        "Main",
        "Favorites",
        "History",
        "PopupBlocked",
        "FindDialog",
        "DeleteBrowsingDataDialog",
        "DownloadProgressDialog",
        "DownloadCompletedDialog",
        "DownloadHistoryDialog",
        "PermissionRequestDialog",
        "PrivacyTab",
        "ContextMenu",
        "HelpMenu",
        "AboutDialog"
    )]
    [string]$State = "Main",

    [string]$OutputPath,

    [switch]$SkipBuild,

    [ValidateRange(5, 120)]
    [int]$TimeoutSeconds = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class VisualTestWindowState
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);
}
'@

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "src\IndianaExpedition.App\IndianaExpedition.App.csproj"
$captureToolConfiguration = "Release"
$visualTestReadyFileArgument = "--visual-test-ready-file"
$windowPollMilliseconds = 100
$renderStabilizationMilliseconds = 200
$horizontalSampleCount = 32
$verticalSampleCount = 24
$maximumDistinctSampleColors = 8
$minimumDistinctSampleColors = 4
$captureToolProjectPath = Join-Path `
    $repositoryRoot `
    "tools\IndianaExpedition.WgcCapture\IndianaExpedition.WgcCapture.csproj"
$captureToolPath = Join-Path `
    $repositoryRoot `
    "tools\IndianaExpedition.WgcCapture\bin\$captureToolConfiguration\net9.0-windows10.0.19041.0\IndianaExpedition.WgcCapture.exe"
$captureDirectory = Join-Path $repositoryRoot "artifacts\wgc"
$profileDirectory = Join-Path $captureDirectory "profile"
$stateFileName = "indiana-expedition-{0}.png" -f $State.ToLowerInvariant()

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $captureDirectory $stateFileName
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot $OutputPath
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $OutputPath
$readyFilePath = [System.IO.Path]::ChangeExtension($OutputPath, ".ready")
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $profileDirectory -Force | Out-Null
Remove-Item -LiteralPath $readyFilePath -Force -ErrorAction SilentlyContinue

if (-not $SkipBuild) {
    dotnet build $projectPath -c $Configuration -p:Platform=x64 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Indiana Expedition $Configuration 빌드에 실패했습니다."
    }

    dotnet build $captureToolProjectPath -c $captureToolConfiguration | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "프로젝트 전용 WGC 캡처 도구 빌드에 실패했습니다."
    }
}

$applicationPath = Join-Path `
    $repositoryRoot `
    "src\IndianaExpedition.App\bin\x64\$Configuration\net48\IndianaExpedition.exe"
if (-not (Test-Path -LiteralPath $applicationPath)) {
    throw "캡처할 실행 파일이 없습니다: $applicationPath"
}
if (-not (Test-Path -LiteralPath $captureToolPath)) {
    throw "WGC 캡처 도구가 없습니다: $captureToolPath"
}

$arguments = @(
    "--visual-test",
    "--visual-state", $State,
    "--visual-test-data-directory", $profileDirectory,
    $visualTestReadyFileArgument, $readyFilePath
)
$process = $null

function Test-ApplicationOwnsForegroundWindow {
    param([int]$ExpectedProcessId)

    $foregroundWindow = [VisualTestWindowState]::GetForegroundWindow()
    if ($foregroundWindow -eq [IntPtr]::Zero) {
        return $false
    }

    [uint32]$foregroundOwnerProcessId = 0
    [VisualTestWindowState]::GetWindowThreadProcessId(
        $foregroundWindow,
        [ref]$foregroundOwnerProcessId) | Out-Null
    return $foregroundOwnerProcessId -eq $ExpectedProcessId
}

try {
    $process = Start-Process `
        -FilePath $applicationPath `
        -ArgumentList $arguments `
        -PassThru

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    [Int64]$windowHandleValue = 0
    do {
        Start-Sleep -Milliseconds $windowPollMilliseconds
        $process.Refresh()
        if ($process.HasExited) {
            throw "Indiana Expedition가 화면 준비 신호를 보내기 전에 종료되었습니다. 종료 코드: $($process.ExitCode)"
        }
        if (Test-ApplicationOwnsForegroundWindow -ExpectedProcessId $process.Id) {
            throw "비간섭 검증 실패: 화면 준비 중 Indiana Expedition의 창이 포그라운드가 되었습니다."
        }

        if (Test-Path -LiteralPath $readyFilePath) {
            try {
                $readyFileContent = (Get-Content -LiteralPath $readyFilePath -Raw).Trim()
                [Int64]::TryParse($readyFileContent, [ref]$windowHandleValue) | Out-Null
            } catch [System.IO.IOException] {
                $windowHandleValue = 0
            }
        }
    } while ($windowHandleValue -le 0 -and [DateTime]::UtcNow -lt $deadline)

    if ($windowHandleValue -le 0) {
        throw "제한 시간 내에 Indiana Expedition 캡처 대상 HWND를 받지 못했습니다."
    }

    $windowHandle = [IntPtr]$windowHandleValue
    if (-not [VisualTestWindowState]::IsWindow($windowHandle)) {
        throw "앱이 기록한 캡처 대상 HWND가 더 이상 유효하지 않습니다: $windowHandleValue"
    }

    [uint32]$windowOwnerProcessId = 0
    $windowThreadId = [VisualTestWindowState]::GetWindowThreadProcessId(
        $windowHandle,
        [ref]$windowOwnerProcessId)
    if ($windowThreadId -eq 0 -or $windowOwnerProcessId -ne $process.Id) {
        throw "캡처 대상 HWND가 실행한 Indiana Expedition 프로세스 소유가 아닙니다. HWND=$windowHandleValue, Owner=$windowOwnerProcessId, Expected=$($process.Id)"
    }

    Start-Sleep -Milliseconds $renderStabilizationMilliseconds
    if (Test-ApplicationOwnsForegroundWindow -ExpectedProcessId $process.Id) {
        throw "비간섭 검증 실패: Indiana Expedition의 창이 포그라운드가 되었습니다."
    }

    $captureOutput = & $captureToolPath `
        --window ([Int64]$windowHandle) `
        --output $OutputPath `
        --timeout-seconds $TimeoutSeconds 2>&1
    $captureExitCode = $LASTEXITCODE
    $captureText = ($captureOutput | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
    if ($captureExitCode -ne 0) {
        throw "직접 WGC 캡처에 실패했습니다. 다른 캡처 방식으로 우회하지 않았습니다.`n$captureText"
    }

    if (Test-ApplicationOwnsForegroundWindow -ExpectedProcessId $process.Id) {
        throw "비간섭 검증 실패: WGC 캡처가 Indiana Expedition의 창을 포그라운드로 전환했습니다."
    }

    if (-not (Test-Path -LiteralPath $OutputPath)) {
        throw "WGC 도구가 PNG 파일을 생성하지 않았습니다: $OutputPath"
    }

    $metadataPath = [System.IO.Path]::ChangeExtension($OutputPath, ".capture.json")
    if (-not [string]::IsNullOrWhiteSpace($captureText)) {
        [System.IO.File]::WriteAllText($metadataPath, $captureText)
    }

    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::FromFile($OutputPath)
    try {
        if ($bitmap.Width -le 0 -or $bitmap.Height -le 0) {
            throw "생성된 PNG의 크기가 올바르지 않습니다."
        }

        $distinctColors = New-Object 'System.Collections.Generic.HashSet[int]'
        $sampleStepX = [Math]::Max(1, [int]($bitmap.Width / $horizontalSampleCount))
        $sampleStepY = [Math]::Max(1, [int]($bitmap.Height / $verticalSampleCount))
        for ($y = 0; $y -lt $bitmap.Height -and $distinctColors.Count -lt $maximumDistinctSampleColors; $y += $sampleStepY) {
            for ($x = 0; $x -lt $bitmap.Width -and $distinctColors.Count -lt $maximumDistinctSampleColors; $x += $sampleStepX) {
                $distinctColors.Add($bitmap.GetPixel($x, $y).ToArgb()) | Out-Null
            }
        }

        if ($distinctColors.Count -lt $minimumDistinctSampleColors) {
            throw "WGC 캡처가 단색 또는 검은 화면입니다. 서로 다른 표본 색상: $($distinctColors.Count)"
        }

        try {
            $captureResult = $captureText | ConvertFrom-Json
        } catch {
            throw "WGC 도구가 올바른 JSON 결과를 반환하지 않았습니다.`n$captureText"
        }

        if (-not ($captureResult.PSObject.Properties.Name -contains "mode") -or
            -not [string]::Equals($captureResult.mode, "wgc", [StringComparison]::OrdinalIgnoreCase)) {
            throw "Windows Graphics Capture 실행 증명이 결과에 없습니다."
        }

        [PSCustomObject]@{
            Path = $OutputPath
            Width = $bitmap.Width
            Height = $bitmap.Height
            State = $State
            CaptureMode = $captureResult.mode
            ApiReportedSupported = $captureResult.reportedSupported
            ForegroundUntouched = $true
            DistinctSampleColors = $distinctColors.Count
            MetadataPath = if (Test-Path -LiteralPath $metadataPath) { $metadataPath } else { $null }
        }
    } finally {
        $bitmap.Dispose()
    }
} finally {
    Remove-Item -LiteralPath $readyFilePath -Force -ErrorAction SilentlyContinue
    if ($null -ne $process) {
        $process.Refresh()
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id
            $process.WaitForExit(5000) | Out-Null
        }
        $process.Dispose()
    }
}
