using System;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.WebView
{
    internal sealed class WebViewEventBindings
    {
        internal EventHandler<CoreWebView2NavigationStartingEventArgs> NavigationStarting { get; set; }
        internal EventHandler<CoreWebView2NavigationCompletedEventArgs> NavigationCompleted { get; set; }
        internal EventHandler<CoreWebView2SourceChangedEventArgs> SourceChanged { get; set; }
        internal EventHandler<object> DocumentTitleChanged { get; set; }
        internal EventHandler<object> HistoryChanged { get; set; }
        internal EventHandler<object> StatusBarTextChanged { get; set; }
        internal EventHandler<CoreWebView2NewWindowRequestedEventArgs> NewWindowRequested { get; set; }
        internal EventHandler<CoreWebView2DownloadStartingEventArgs> DownloadStarting { get; set; }
        internal EventHandler<CoreWebView2ProcessFailedEventArgs> ProcessFailed { get; set; }
        internal EventHandler<CoreWebView2PermissionRequestedEventArgs> PermissionRequested { get; set; }
        internal EventHandler<CoreWebView2ContextMenuRequestedEventArgs> ContextMenuRequested { get; set; }

        internal void Attach(CoreWebView2 core)
        {
            core.NavigationStarting += NavigationStarting;
            core.NavigationCompleted += NavigationCompleted;
            core.SourceChanged += SourceChanged;
            core.DocumentTitleChanged += DocumentTitleChanged;
            core.HistoryChanged += HistoryChanged;
            core.StatusBarTextChanged += StatusBarTextChanged;
            core.NewWindowRequested += NewWindowRequested;
            core.DownloadStarting += DownloadStarting;
            core.ProcessFailed += ProcessFailed;
            core.PermissionRequested += PermissionRequested;
            core.ContextMenuRequested += ContextMenuRequested;
        }

        internal void Detach(CoreWebView2 core)
        {
            if (core == null)
            {
                return;
            }
            core.NavigationStarting -= NavigationStarting;
            core.NavigationCompleted -= NavigationCompleted;
            core.SourceChanged -= SourceChanged;
            core.DocumentTitleChanged -= DocumentTitleChanged;
            core.HistoryChanged -= HistoryChanged;
            core.StatusBarTextChanged -= StatusBarTextChanged;
            core.NewWindowRequested -= NewWindowRequested;
            core.DownloadStarting -= DownloadStarting;
            core.ProcessFailed -= ProcessFailed;
            core.PermissionRequested -= PermissionRequested;
            core.ContextMenuRequested -= ContextMenuRequested;
        }
    }
}
