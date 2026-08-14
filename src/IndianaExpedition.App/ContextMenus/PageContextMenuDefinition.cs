using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IndianaExpedition.Constants;

namespace IndianaExpedition.ContextMenus
{
    internal sealed class PageContextMenuDefinition
    {
        private readonly Dictionary<ToolStripItem, Action> _commands =
            new Dictionary<ToolStripItem, Action>();
        private readonly Dictionary<PageContextMenuCommand, ToolStripItem> _items =
            new Dictionary<PageContextMenuCommand, ToolStripItem>();

        internal PageContextMenuDefinition(ContextMenuStrip menu)
        {
            Menu = menu ?? throw new ArgumentNullException(nameof(menu));
        }

        internal ContextMenuStrip Menu { get; }

        internal event EventHandler<PageContextMenuCommandEventArgs> CommandInvoked;

        internal ToolStripItem AddCommand(
            PageContextMenuCommand command,
            string text,
            Action execute,
            bool enabled)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            var item = new ToolStripMenuItem(text)
            {
                Name = UiAutomationIds.ContextMenu.Command(command),
                Enabled = enabled
            };
            item.Click += OnCommandClicked;
            Menu.Items.Add(item);
            _commands.Add(item, execute);
            _items.Add(command, item);
            return item;
        }

        internal void AddSeparator()
        {
            Menu.Items.Add(new ToolStripSeparator());
        }

        internal void AddDisabledItem(PageContextMenuCommand command, string text)
        {
            var item = new ToolStripMenuItem(text)
            {
                Name = UiAutomationIds.ContextMenu.Command(command),
                Enabled = false
            };
            Menu.Items.Add(item);
            _items.Add(command, item);
        }

        internal ToolStripItem GetItem(PageContextMenuCommand command)
        {
            if (!_items.TryGetValue(command, out var item))
            {
                throw new InvalidOperationException("The page context-menu item is not available: " + command);
            }

            return item;
        }

        internal bool Contains(PageContextMenuCommand command)
        {
            return _items.ContainsKey(command);
        }

        private void OnCommandClicked(object sender, EventArgs args)
        {
            if (sender is ToolStripItem item &&
                item.Enabled &&
                _commands.TryGetValue(item, out var execute))
            {
                CommandInvoked?.Invoke(this, new PageContextMenuCommandEventArgs(execute));
            }
        }
    }

    internal sealed class PageContextMenuCommandEventArgs : EventArgs
    {
        internal PageContextMenuCommandEventArgs(Action execute)
        {
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        internal Action Execute { get; }
    }
}
