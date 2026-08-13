using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.ContextMenus
{
    internal sealed class WebViewContextMenuSession : IDisposable
    {
        private readonly ContextMenuStrip _menu;
        private readonly CoreWebView2Deferral _deferral;
        private bool _disposed;

        internal WebViewContextMenuSession(
            ContextMenuStrip menu,
            CoreWebView2Deferral deferral)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _deferral = deferral ?? throw new ArgumentNullException(nameof(deferral));
            _menu.Closed += OnMenuClosed;
        }

        internal event EventHandler Closed;

        internal void Show(Control owner, Point location)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebViewContextMenuSession));
            }
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            _menu.Show(owner, location);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _menu.Closed -= OnMenuClosed;
            try
            {
                _menu.Dispose();
            }
            finally
            {
                try
                {
                    _deferral.Complete();
                }
                finally
                {
                    _deferral.Dispose();
                }
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void OnMenuClosed(object sender, ToolStripDropDownClosedEventArgs args)
        {
            Dispose();
        }
    }
}
