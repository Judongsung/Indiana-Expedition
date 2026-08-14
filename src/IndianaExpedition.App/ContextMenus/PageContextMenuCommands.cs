using System;
using System.Collections.Generic;

namespace IndianaExpedition.ContextMenus
{
    internal enum PageContextMenuCommand
    {
        Back,
        Forward,
        Refresh,
        OpenLinkNewWindow,
        CopyLink,
        CopySelection,
        SelectAll,
        Properties
    }

    internal sealed class PageContextMenuCommandBinding
    {
        internal PageContextMenuCommandBinding(Action execute, bool enabled)
        {
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
            Enabled = enabled;
        }

        internal Action Execute { get; }

        internal bool Enabled { get; }
    }

    internal sealed class PageContextMenuCommandMap
    {
        private readonly Dictionary<PageContextMenuCommand, PageContextMenuCommandBinding> _bindings =
            new Dictionary<PageContextMenuCommand, PageContextMenuCommandBinding>();

        internal void Add(PageContextMenuCommand command, Action execute, bool enabled = true)
        {
            _bindings.Add(command, new PageContextMenuCommandBinding(execute, enabled));
        }

        internal PageContextMenuCommandBinding Get(PageContextMenuCommand command)
        {
            if (!_bindings.TryGetValue(command, out var binding))
            {
                throw new InvalidOperationException("A page context-menu command binding is missing: " + command);
            }

            return binding;
        }
    }
}
