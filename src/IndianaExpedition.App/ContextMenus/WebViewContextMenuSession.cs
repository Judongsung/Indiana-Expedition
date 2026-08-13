using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.ContextMenus
{
    internal sealed class WebViewContextMenuSession : IDisposable
    {
        private readonly PageContextMenuDefinition _definition;
        private readonly ContextMenuStrip _menu;
        private readonly CoreWebView2Deferral _deferral;
        private Control _owner;
        private Action _selectedCommand;
        private bool _disposed;

        internal WebViewContextMenuSession(
            PageContextMenuDefinition definition,
            CoreWebView2Deferral deferral)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _menu = definition.Menu;
            _deferral = deferral ?? throw new ArgumentNullException(nameof(deferral));
            _menu.ItemClicked += OnItemClicked;
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

            _owner = owner;
            _menu.Show(owner, location);
        }

        public void Dispose()
        {
            CompleteSession(command: null);
        }

        private void OnItemClicked(object sender, ToolStripItemClickedEventArgs args)
        {
            _definition.TryGetCommand(args.ClickedItem, out _selectedCommand);
        }

        private void OnMenuClosed(object sender, ToolStripDropDownClosedEventArgs args)
        {
            CompleteSession(_selectedCommand);
        }

        private void CompleteSession(Action command)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _menu.ItemClicked -= OnItemClicked;
            _menu.Closed -= OnMenuClosed;
            try
            {
                _deferral.Complete();
            }
            finally
            {
                Closed?.Invoke(this, EventArgs.Empty);
                DispatchAfterContextMenuRequest(_owner, _menu, command);
            }
        }

        private static void DispatchAfterContextMenuRequest(
            Control owner,
            ContextMenuStrip menu,
            Action command)
        {
            if (owner == null || owner.IsDisposed || !owner.IsHandleCreated)
            {
                menu.Dispose();
                return;
            }

            try
            {
                owner.BeginInvoke(new MethodInvoker(() =>
                {
                    try
                    {
                        menu.Dispose();
                    }
                    finally
                    {
                        command?.Invoke();
                    }
                }));
            }
            catch (InvalidOperationException)
            {
                menu.Dispose();
            }
        }
    }
}
