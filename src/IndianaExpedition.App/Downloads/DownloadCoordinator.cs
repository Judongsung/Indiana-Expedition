using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using IndianaExpedition.Browser;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Dialogs;
using IndianaExpedition.Resources;
using IndianaExpedition.Commands;

namespace IndianaExpedition.Downloads
{
    internal sealed class DownloadCoordinator : IDisposable
    {
        private readonly SettingsService _settings;
        private readonly DownloadHistoryService _history;
        private readonly IDownloadHistoryController _historyController;
        private readonly IExternalLauncher _externalLauncher;
        private readonly Dictionary<BrowserForm, HashSet<DownloadSession>> _activeSessions =
            new Dictionary<BrowserForm, HashSet<DownloadSession>>();
        private readonly Dictionary<DownloadSession, DownloadProgressDialog> _progressDialogs =
            new Dictionary<DownloadSession, DownloadProgressDialog>();
        private DownloadHistoryDialog _historyDialog;
        private bool _disposed;

        internal DownloadCoordinator(
            SettingsService settings,
            DownloadHistoryService history,
            IExternalLauncher externalLauncher = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _historyController = new DownloadHistoryController(history);
            _externalLauncher = externalLauncher ?? new ShellExternalLauncher();
        }

        internal void StartDownload(
            BrowserForm owner,
            CoreWebView2DownloadStartingEventArgs args)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            args.Handled = true;
            try
            {
                var targetPath = DownloadPathResolver.Resolve(
                    owner,
                    _settings.Current,
                    args.ResultFilePath);
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    args.Cancel = true;
                    return;
                }

                args.ResultFilePath = targetPath;
                var session = new DownloadSession(
                    new WebViewDownloadOperation(args.DownloadOperation),
                    DateTime.UtcNow);
                RegisterSession(owner, session);
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                MessageBox.Show(
                    ex.Message,
                    Branding.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        internal void ShowHistory(BrowserForm owner)
        {
            if (_historyDialog != null && !_historyDialog.IsDisposed)
            {
                if (_historyDialog.WindowState == FormWindowState.Minimized)
                {
                    _historyDialog.WindowState = FormWindowState.Normal;
                }
                _historyDialog.Activate();
                return;
            }

            _historyDialog = new DownloadHistoryDialog(
                _historyController,
                externalLauncher: _externalLauncher);
            _historyDialog.FormClosed += OnHistoryDialogClosed;
            _historyDialog.Show(owner);
        }

        internal bool ConfirmOwnerClose(BrowserForm owner, CloseReason reason)
        {
            var sessions = ActiveSessionsFor(owner).ToList();
            if (sessions.Count == 0)
            {
                return true;
            }

            var requiresConfirmation = reason == CloseReason.UserClosing;
            if (requiresConfirmation && !LunaConfirmationDialog.Confirm(
                owner,
                Strings.CancelDownloadsTitle,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.CancelDownloadsPromptFormat,
                    sessions.Count),
                Strings.CancelDownloads))
            {
                return false;
            }

            foreach (var session in sessions)
            {
                session.Cancel();
            }
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var session in _progressDialogs.Keys.ToList())
            {
                if (!session.IsFinished)
                {
                    session.Cancel();
                }
            }
            foreach (var dialog in _progressDialogs.Values.ToList())
            {
                dialog.Close();
                dialog.Dispose();
            }
            _progressDialogs.Clear();
            _activeSessions.Clear();

            if (_historyDialog != null)
            {
                _historyDialog.FormClosed -= OnHistoryDialogClosed;
                _historyDialog.Close();
                _historyDialog.Dispose();
                _historyDialog = null;
            }

            _disposed = true;
        }

        private void RegisterSession(BrowserForm owner, DownloadSession session)
        {
            if (!_activeSessions.TryGetValue(owner, out var sessions))
            {
                sessions = new HashSet<DownloadSession>();
                _activeSessions[owner] = sessions;
            }
            sessions.Add(session);

            session.Changed += (sender, args) => ReportStatus(owner, session);
            session.Finished += (sender, args) => CompleteSession(owner, session);

            var dialog = new DownloadProgressDialog(
                session,
                externalLauncher: _externalLauncher);
            _progressDialogs[session] = dialog;
            dialog.FormClosed += (sender, args) => ReleaseSession(owner, session, dialog);
            dialog.Show(owner);
            ReportStatus(owner, session);

            if (session.IsFinished)
            {
                CompleteSession(owner, session);
            }
        }

        private IEnumerable<DownloadSession> ActiveSessionsFor(BrowserForm owner)
        {
            return owner != null && _activeSessions.TryGetValue(owner, out var sessions)
                ? sessions.ToList()
                : Enumerable.Empty<DownloadSession>();
        }

        private void CompleteSession(BrowserForm owner, DownloadSession session)
        {
            if (_activeSessions.TryGetValue(owner, out var sessions) && sessions.Remove(session))
            {
                if (sessions.Count == 0)
                {
                    _activeSessions.Remove(owner);
                }
                try
                {
                    _history.Add(session.CreateRecord());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.DownloadHistorySaveFailedFormat,
                            ex.Message),
                        Branding.ProductName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            ReportStatus(owner, session);
        }

        private void ReleaseSession(
            BrowserForm owner,
            DownloadSession session,
            DownloadProgressDialog dialog)
        {
            if (!session.IsFinished)
            {
                session.Cancel();
            }

            if (_activeSessions.TryGetValue(owner, out var sessions))
            {
                sessions.Remove(session);
                if (sessions.Count == 0)
                {
                    _activeSessions.Remove(owner);
                }
            }
            _progressDialogs.Remove(session);
            session.Dispose();
            dialog.Dispose();
        }

        private static void ReportStatus(BrowserForm owner, DownloadSession session)
        {
            if (owner == null || owner.IsDisposed)
            {
                return;
            }
            owner.SetDownloadStatus(DownloadDisplayFormatter.FormatBrowserStatus(
                session.State,
                session.FileName));
        }

        private void OnHistoryDialogClosed(object sender, FormClosedEventArgs args)
        {
            if (_historyDialog != null)
            {
                _historyDialog.FormClosed -= OnHistoryDialogClosed;
                _historyDialog.Dispose();
                _historyDialog = null;
            }
        }
    }
}
