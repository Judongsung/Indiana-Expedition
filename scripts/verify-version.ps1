[CmdletBinding()]
param(
    [string]$ExpectedVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $repositoryRoot "Directory.Build.props"
$readmePath = Join-Path $repositoryRoot "README.md"
$releaseReadmePath = Join-Path $repositoryRoot "docs\RELEASE-README.txt"
$appProjectPath = Join-Path $repositoryRoot "src\IndianaExpedition.App\IndianaExpedition.App.csproj"
$coreProjectPath = Join-Path $repositoryRoot "src\IndianaExpedition.Core\IndianaExpedition.Core.csproj"
$versionPattern = '^\d+\.\d+\.\d+$'
$versionReference = '$(IndianaExpeditionVersion)'

[xml]$props = Get-Content -LiteralPath $propsPath -Raw
$versionNode = $props.SelectSingleNode("/Project/PropertyGroup/IndianaExpeditionVersion")
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    throw "Directory.Build.props에서 IndianaExpeditionVersion을 찾지 못했습니다."
}

$sourceVersion = $versionNode.InnerText.Trim()
if ($sourceVersion -notmatch $versionPattern) {
    throw "공통 버전은 major.minor.patch 형식이어야 합니다: $sourceVersion"
}
if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion) -and
    -not [string]::Equals($sourceVersion, $ExpectedVersion, [StringComparison]::Ordinal)) {
    throw "요청 버전과 소스 버전이 다릅니다. Requested=$ExpectedVersion, Source=$sourceVersion"
}

$readme = Get-Content -LiteralPath $readmePath -Raw
$readmeMatch = [regex]::Match($readme, '현재 버전 `(?<version>\d+\.\d+\.\d+)`')
if (-not $readmeMatch.Success -or
    -not [string]::Equals(
        $sourceVersion,
        $readmeMatch.Groups['version'].Value,
        [StringComparison]::Ordinal)) {
    throw "README.md의 현재 버전이 공통 버전과 다릅니다."
}

$releaseReadme = Get-Content -LiteralPath $releaseReadmePath -Raw
$releaseReadmeMatch = [regex]::Match(
    $releaseReadme,
    '^Indiana Expedition (?<version>\d+\.\d+\.\d+)',
    [System.Text.RegularExpressions.RegexOptions]::Multiline)
if (-not $releaseReadmeMatch.Success -or
    -not [string]::Equals(
        $sourceVersion,
        $releaseReadmeMatch.Groups['version'].Value,
        [StringComparison]::Ordinal)) {
    throw "docs/RELEASE-README.txt의 버전이 공통 버전과 다릅니다."
}

foreach ($projectPath in @($appProjectPath, $coreProjectPath)) {
    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    $projectVersion = $project.SelectSingleNode("/Project/PropertyGroup/Version")
    if ($null -eq $projectVersion -or
        -not [string]::Equals(
            $versionReference,
            $projectVersion.InnerText.Trim(),
            [StringComparison]::Ordinal)) {
        throw "프로젝트가 공통 버전을 참조하지 않습니다: $projectPath"
    }
}

[PSCustomObject]@{
    Version = $sourceVersion
    PropsPath = $propsPath
}
