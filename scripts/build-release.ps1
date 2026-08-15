param(
    [string]$Configuration = "Release",

    [switch]$NoRestore,

    [switch]$StageOnly
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "release-layout.ps1")
$resolveMsBuildScript = Join-Path $PSScriptRoot "resolve-msbuild.ps1"
$msbuildPath = & $resolveMsBuildScript
$project = Join-Path $repositoryRoot "src\IndianaExpedition.App\IndianaExpedition.App.csproj"
$sourceOutputDirectory = Join-Path `
    $repositoryRoot `
    "src\IndianaExpedition.App\bin\x64\$Configuration\net48"
$artifactsRoot = Join-Path $repositoryRoot $releaseLayout.ArtifactsDirectoryName
$artifactDirectory = Join-Path $artifactsRoot $releaseLayout.ReleaseDirectoryName
$resolvedArtifactsRoot = [System.IO.Path]::GetFullPath($artifactsRoot)
$resolvedArtifactDirectory = [System.IO.Path]::GetFullPath($artifactDirectory)
$artifactsRootPrefix = $resolvedArtifactsRoot.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

if (-not $resolvedArtifactDirectory.StartsWith(
    $artifactsRootPrefix,
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Release 폴더가 artifacts 범위를 벗어났습니다: $resolvedArtifactDirectory"
}

if (Test-Path -LiteralPath $resolvedArtifactDirectory) {
    $artifactItem = Get-Item -LiteralPath $resolvedArtifactDirectory -Force
    if (($artifactItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Release 폴더가 재분석 지점이므로 초기화하지 않습니다: $resolvedArtifactDirectory"
    }

    Remove-Item -LiteralPath $resolvedArtifactDirectory -Recurse -Force
}

if (-not $StageOnly) {
    $buildArguments = @(
        $project,
        "/m",
        "/nr:false",
        "/t:Build",
        "/p:Configuration=$Configuration",
        "/p:Platform=x64",
        "/verbosity:minimal"
    )
    if (-not $NoRestore) {
        $buildArguments += "/restore"
    }
    & $msbuildPath @buildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Indiana Expedition Release 빌드에 실패했습니다."
    }
}

if (-not (Test-Path -LiteralPath $sourceOutputDirectory)) {
    throw "검증된 App 출력 폴더가 없습니다: $sourceOutputDirectory"
}

New-Item -ItemType Directory -Path $resolvedArtifactDirectory -Force | Out-Null
foreach ($file in $releaseLayout.RequiredFiles) {
    $sourcePath = Join-Path $sourceOutputDirectory $file
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "App 출력에 배포 파일이 없습니다: $file"
    }
    $destinationPath = Join-Path $resolvedArtifactDirectory $file
    $destinationParent = Split-Path -Parent $destinationPath
    New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
    Copy-Item -LiteralPath $sourcePath -Destination $destinationPath
}

foreach ($file in $releaseLayout.RequiredFiles) {
    $path = Join-Path $resolvedArtifactDirectory $file
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Release 파일이 없습니다: $file"
    }
    if ((Get-Item -LiteralPath $path).Length -le 0) {
        throw "Release 파일이 비어 있습니다: $file"
    }
}

$licenseText = Get-Content -LiteralPath (Join-Path $resolvedArtifactDirectory "LICENSE") -Raw
if (-not $licenseText.Contains("MIT License")) {
    throw "배포 LICENSE에서 MIT License 표제를 찾지 못했습니다."
}

$thirdPartyNotice = Get-Content `
    -LiteralPath (Join-Path $resolvedArtifactDirectory "THIRD-PARTY-NOTICES.md") `
    -Raw
if (-not $thirdPartyNotice.Contains("WebView2")) {
    throw "배포 제3자 고지에서 WebView2 항목을 찾지 못했습니다."
}

Write-Host "Indiana Expedition Release 폴더: $resolvedArtifactDirectory"
