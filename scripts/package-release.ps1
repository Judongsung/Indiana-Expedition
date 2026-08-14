[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "release-layout.ps1")
$artifactsRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot $releaseLayout.ArtifactsDirectoryName))
$releaseDirectory = Join-Path $artifactsRoot $releaseLayout.ReleaseDirectoryName
$verifyVersionScript = Join-Path $PSScriptRoot "verify-version.ps1"
$versionResult = & $verifyVersionScript -ExpectedVersion $Version
$packageName = [string]::Format(
    [Globalization.CultureInfo]::InvariantCulture,
    $releaseLayout.PackageNameFormat,
    $versionResult.Version)
$zipPath = Join-Path $artifactsRoot $packageName
$checksumPath = $zipPath + ".sha256"

if (-not (Test-Path -LiteralPath $releaseDirectory)) {
    throw "배포 폴더가 없습니다. 먼저 scripts/verify.ps1을 실행하세요: $releaseDirectory"
}

foreach ($targetPath in @($zipPath, $checksumPath)) {
    $resolvedTarget = [System.IO.Path]::GetFullPath($targetPath)
    $artifactsPrefix = $artifactsRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTarget.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "패키지 대상이 artifacts 범위를 벗어났습니다: $resolvedTarget"
    }
    if (Test-Path -LiteralPath $resolvedTarget) {
        Remove-Item -LiteralPath $resolvedTarget -Force
    }
}

Compress-Archive `
    -Path (Join-Path $releaseDirectory "*") `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entryNames = @($archive.Entries | ForEach-Object {
        $_.FullName.Replace('/', '\')
    })
    foreach ($requiredFile in $releaseLayout.RequiredFiles) {
        if ($entryNames -notcontains $requiredFile) {
            throw "Release ZIP에 필수 파일이 없습니다: $requiredFile"
        }
    }
} finally {
    $archive.Dispose()
}

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
$checksumLine = $hash + "  " + [System.IO.Path]::GetFileName($zipPath) + [Environment]::NewLine
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText(
    $checksumPath,
    $checksumLine,
    $utf8WithoutBom)

$recordedHash = ((Get-Content -LiteralPath $checksumPath -Raw).Trim() -split '\s+')[0]
if (-not [string]::Equals($hash, $recordedHash, [StringComparison]::OrdinalIgnoreCase)) {
    throw "생성된 SHA-256 파일이 ZIP 해시와 일치하지 않습니다."
}

[PSCustomObject]@{
    Version = $versionResult.Version
    ZipPath = [System.IO.Path]::GetFullPath($zipPath)
    ChecksumPath = [System.IO.Path]::GetFullPath($checksumPath)
    Sha256 = $hash
}
