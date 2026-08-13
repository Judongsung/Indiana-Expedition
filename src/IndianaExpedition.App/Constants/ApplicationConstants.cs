namespace IndianaExpedition.Constants
{
    internal static class ApplicationConstants
    {
        internal const int WindowMessageKeyDown = 0x0100;
        internal const int WindowMessageSystemKeyDown = 0x0104;
        internal const string DataDirectoryName = "IndianaExpedition";
        internal const string WebView2BrowserProjectUrl =
            "https://github.com/MicrosoftEdge/WebView2Browser";
        internal const string ProjectRepositoryUrl =
            "https://github.com/Judongsung/Indiana-Expedition";
        internal const string WebView2DownloadUrl =
            "https://developer.microsoft.com/microsoft-edge/webview2/";
        internal const string WindowTitleSeparator = " - ";
        internal const string VisualTestModeArgument = "--visual-test";
        internal const string VisualTestStateArgument = "--visual-state";
        internal const string VisualTestDataDirectoryArgument = "--visual-test-data-directory";
        internal const string VisualTestReadyFileArgument = "--visual-test-ready-file";
        internal const string VisualTestDataDirectoryName = "IndianaExpedition.VisualTest";
    }

    internal static class BrowserUiConstants
    {
        internal const string DefaultDownloadFileName = "download";
        internal const int MaximumDownloadNameAttempts = 10000;
        internal const string UniqueIdentifierFormat = "N";
        internal const string HistoryTimeFormat = "HH:mm";
        internal const string HistoryDateFormat = "yyyy년 M월 d일 dddd";
        internal const string UniqueDownloadNameFormat = "{0} ({1}){2}";
        internal const char FolderIndentCharacter = '　';
        internal const string CloseGlyph = "×";
        internal const int FolderImageIndex = 0;
        internal const int PageImageIndex = 1;
        internal const int HistoryImageIndex = 2;
        internal const int FavoriteMenuCommandCount = 3;
    }

    internal static class BrowserScriptConstants
    {
        internal const string ExecuteCommandTemplate = "document.execCommand('{0}');";
        internal const string CutCommand = "cut";
        internal const string CopyCommand = "copy";
        internal const string PasteCommand = "paste";
        internal const string SelectAllCommand = "selectAll";
    }

    internal static class VisualTestConstants
    {
        internal const string PopupSourceOrigin = "https://example.com";
        internal const string PopupTargetUrl = "https://example.com/popup";
        internal const string FindTerm = "Windows XP";
        internal const int FindActiveMatchIndex = 2;
        internal const int FindMatchCount = 5;
    }
}
