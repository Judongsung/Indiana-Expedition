using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace IndianaExpedition.WebView
{
    internal sealed class WebViewHostController : IDisposable
    {
        private readonly Panel _host;
        private readonly Task<CoreWebView2Environment> _environmentTask;
        private readonly WebViewEventBindings _events;
        private bool _disposed;

        internal WebViewHostController(
            Panel host,
            Task<CoreWebView2Environment> environmentTask,
            WebViewEventBindings events)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _environmentTask = environmentTask ?? throw new ArgumentNullException(nameof(environmentTask));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        internal WebView2 Current { get; private set; }

        internal async Task<WebView2> CreateAsync()
        {
            ThrowIfDisposed();
            ReleaseCurrent();
            foreach (var child in _host.Controls.Cast<Control>().ToArray())
            {
                _host.Controls.Remove(child);
                child.Dispose();
            }
            var candidate = new WebView2
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                DefaultBackgroundColor = Color.White
            };
            Current = candidate;
            _host.Controls.Clear();
            _host.Controls.Add(candidate);
            try
            {
                var environment = await _environmentTask.ConfigureAwait(true);
                await candidate.EnsureCoreWebView2Async(environment).ConfigureAwait(true);
                Configure(candidate.CoreWebView2);
                _events.Attach(candidate.CoreWebView2);
                return candidate;
            }
            catch
            {
                ReleaseCurrent();
                throw;
            }
        }

        internal void ReleaseCurrent()
        {
            var current = Current;
            Current = null;
            if (current == null)
            {
                return;
            }
            _events.Detach(current.CoreWebView2);
            _host.Controls.Remove(current);
            current.Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            ReleaseCurrent();
            _disposed = true;
        }

        private static void Configure(CoreWebView2 core)
        {
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
