$releaseLayout = [PSCustomObject]@{
    ArtifactsDirectoryName = "artifacts"
    ReleaseDirectoryName = "IndianaExpedition-win-x64"
    PackageNameFormat = "IndianaExpedition-{0}-win-x64.zip"
    RequiredFiles = @(
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
}
