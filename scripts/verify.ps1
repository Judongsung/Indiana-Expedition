[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repositoryRoot "IndianaExpedition.sln"
$coreTestsPath = Join-Path `
    $repositoryRoot `
    "tests\IndianaExpedition.Core.Tests\bin\Release\net48\IndianaExpedition.Core.Tests.exe"
$appTestsPath = Join-Path `
    $repositoryRoot `
    "tests\IndianaExpedition.App.Tests\bin\x64\Release\net48\IndianaExpedition.App.Tests.exe"
$buildReleaseScript = Join-Path $PSScriptRoot "build-release.ps1"
$verifyVersionScript = Join-Path $PSScriptRoot "verify-version.ps1"

Write-Host "검증 SDK: $(& dotnet --version)"

Write-Host "[1/6] 솔루션 복원"
& dotnet restore $solutionPath "-p:Platform=x64"
if ($LASTEXITCODE -ne 0) {
    throw "솔루션 복원에 실패했습니다."
}

Write-Host "[2/6] Release/x64 솔루션 빌드"
& dotnet build $solutionPath -c Release "-p:Platform=x64" --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Release/x64 솔루션 빌드에 실패했습니다."
}

Write-Host "[3/6] Core 테스트"
& $coreTestsPath
if ($LASTEXITCODE -ne 0) {
    throw "Core 테스트에 실패했습니다."
}

Write-Host "[4/6] App 동작 테스트"
& $appTestsPath
if ($LASTEXITCODE -ne 0) {
    throw "App 동작 테스트에 실패했습니다."
}

Write-Host "[5/6] 배포 폴더와 버전 검사"
& $buildReleaseScript -Configuration Release -NoRestore
& $verifyVersionScript | Out-Host

Write-Host "[6/6] git diff --check"
& git -C $repositoryRoot diff --check
if ($LASTEXITCODE -ne 0) {
    throw "git diff --check에 실패했습니다."
}

Write-Host "PASS: Indiana Expedition 공통 검증이 완료되었습니다."
