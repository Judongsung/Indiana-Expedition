using System;
using System.Windows.Forms;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition.ContextMenus
{
    internal static class PageContextMenuFactory
    {
        internal static PageContextMenuDefinition Create(
            PageContextMenuModel model,
            PageContextMenuCommandMap commands)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            var definition = new PageContextMenuDefinition(
                new ContextMenuStrip { Renderer = new XpToolStripRenderer() });
            AddCommand(definition, commands, PageContextMenuCommand.Back, Strings.ContextBack);
            AddCommand(definition, commands, PageContextMenuCommand.Forward, Strings.ContextForward);
            AddCommand(definition, commands, PageContextMenuCommand.Refresh, Strings.ContextRefresh);
            definition.AddSeparator();

            if (model.HasLink)
            {
                AddCommand(
                    definition,
                    commands,
                    PageContextMenuCommand.OpenLinkNewWindow,
                    Strings.ContextOpenLinkNewWindow);
                AddCommand(
                    definition,
                    commands,
                    PageContextMenuCommand.CopyLink,
                    Strings.ContextCopyShortcut);
                definition.AddSeparator();
            }

            AddCommand(
                definition,
                commands,
                PageContextMenuCommand.CopySelection,
                Strings.ContextCopy,
                model.HasSelection);
            AddCommand(definition, commands, PageContextMenuCommand.SelectAll, Strings.ContextSelectAll);
            definition.AddSeparator();
            definition.AddDisabledItem(PageContextMenuCommand.Properties, Strings.ContextProperties);
            return definition;
        }

        private static void AddCommand(
            PageContextMenuDefinition definition,
            PageContextMenuCommandMap commands,
            PageContextMenuCommand command,
            string text,
            bool contextEnabled = true)
        {
            var binding = commands.Get(command);
            definition.AddCommand(
                command,
                text,
                binding.Execute,
                contextEnabled && binding.Enabled);
        }
    }
}
