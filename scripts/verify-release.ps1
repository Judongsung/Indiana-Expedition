[CmdletBinding()]
param(
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$verifyScript = Join-Path $PSScriptRoot "verify.ps1"
$verifyVersionScript = Join-Path $PSScriptRoot "verify-version.ps1"
$visualTestScript = Join-Path $PSScriptRoot "test-visual.ps1"
$packageScript = Join-Path $PSScriptRoot "package-release.ps1"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$smokeTestPath = Join-Path `
    $repositoryRoot `
    "tests\IndianaExpedition.WebViewSmokeTests\bin\x64\Release\net48\IndianaExpedition.WebViewSmokeTests.exe"
$versionResult = & $verifyVersionScript -ExpectedVersion $Version

& $verifyScript

& $smokeTestPath
if ($LASTEXITCODE -ne 0) {
    throw "로컬 WebView2 smoke 테스트에 실패했습니다."
}

$visualResults = @(& $visualTestScript -Configuration Release -SkipBuild)
if ($visualResults.Count -ne 14) {
    throw "WGC 결과 수가 14개가 아닙니다: $($visualResults.Count)"
}
foreach ($result in $visualResults) {
    if (-not [string]::Equals($result.CaptureMode, "wgc", [StringComparison]::OrdinalIgnoreCase) -or
        $result.ForegroundUntouched -ne $true -or
        $result.BaselinePassed -ne $true) {
        throw "WGC 릴리스 게이트를 통과하지 못했습니다: $($result.State)"
    }
}
$visualResults |
    Select-Object State, CaptureMode, ForegroundUntouched, BaselinePassed, ChangedPixelRatio, MeanAbsoluteRgbError, Width, Height |
    Format-Table |
    Out-Host

$package = & $packageScript -Version $versionResult.Version
$package | Format-List | Out-Host
Write-Host "PASS: WGC 14개 상태와 Release 패키지 검증이 완료되었습니다."
