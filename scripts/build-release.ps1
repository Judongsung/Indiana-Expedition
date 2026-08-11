param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot "src\IndianaExpedition.App\IndianaExpedition.App.csproj"
$artifactsRoot = Join-Path $repositoryRoot "artifacts"
$artifactDirectory = Join-Path $artifactsRoot "IndianaExpedition-win-x64"
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

New-Item -ItemType Directory -Path $resolvedArtifactDirectory -Force | Out-Null

dotnet build $project `
    -c $Configuration `
    -p:Platform=x64 `
    -p:OutDir="$resolvedArtifactDirectory\"

if ($LASTEXITCODE -ne 0) {
    throw "Indiana Expedition Release 빌드에 실패했습니다."
}

$requiredFiles = @(
    "IndianaExpedition.exe",
    "IndianaExpedition.exe.config",
    "IndianaExpedition.Core.dll",
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll",
    "LICENSE",
    "README.txt",
    "THIRD-PARTY-NOTICES.md",
    "licenses\Microsoft.Web.WebView2-LICENSE.txt",
    "licenses\Microsoft.Web.WebView2-NOTICE.txt"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $resolvedArtifactDirectory $file
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Release 파일이 없습니다: $file"
    }
}

Write-Host "Indiana Expedition Release 폴더: $resolvedArtifactDirectory"
