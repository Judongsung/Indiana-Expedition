[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$visualStudioInstallerPath = Join-Path `
    ${env:ProgramFiles(x86)} `
    "Microsoft Visual Studio\Installer"
$vswherePath = Join-Path $visualStudioInstallerPath "vswhere.exe"
$msbuildComponentId = "Microsoft.Component.MSBuild"
$msbuildSearchPattern = "MSBuild\**\Bin\MSBuild.exe"

if (-not (Test-Path -LiteralPath $vswherePath)) {
    throw "Visual Studio 설치 검색 도구를 찾지 못했습니다: $vswherePath"
}

$msbuildPaths = @(
    & $vswherePath `
        -latest `
        -products * `
        -requires $msbuildComponentId `
        -find $msbuildSearchPattern
)
if ($LASTEXITCODE -ne 0) {
    throw "Visual Studio MSBuild 검색에 실패했습니다."
}

$msbuildPath = $msbuildPaths | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($msbuildPath) -or
    -not (Test-Path -LiteralPath $msbuildPath)) {
    throw "Visual Studio MSBuild를 찾지 못했습니다."
}

[System.IO.Path]::GetFullPath($msbuildPath)
