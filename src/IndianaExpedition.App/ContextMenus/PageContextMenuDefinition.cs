using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IndianaExpedition.ContextMenus
{
    internal sealed class PageContextMenuDefinition
    {
        private readonly Dictionary<ToolStripItem, Action> _commands =
            new Dictionary<ToolStripItem, Action>();

        internal PageContextMenuDefinition(ContextMenuStrip menu)
        {
            Menu = menu ?? throw new ArgumentNullException(nameof(menu));
        }

        internal ContextMenuStrip Menu { get; }

        internal ToolStripItem AddCommand(string text, Action command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var item = Menu.Items.Add(text);
            _commands.Add(item, command);
            return item;
        }

        internal void AddSeparator()
        {
            Menu.Items.Add(new ToolStripSeparator());
        }

        internal void AddDisabledItem(string text)
        {
            Menu.Items.Add(new ToolStripMenuItem(text) { Enabled = false });
        }

        internal bool TryGetCommand(ToolStripItem item, out Action command)
        {
            if (item == null)
            {
                command = null;
                return false;
            }

            return _commands.TryGetValue(item, out command);
        }
    }
}
