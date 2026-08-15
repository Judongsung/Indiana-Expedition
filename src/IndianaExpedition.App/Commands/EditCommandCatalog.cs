using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Commands
{
    internal enum EditCommand
    {
        Cut,
        Copy,
        Paste,
        SelectAll
    }

    internal static class EditCommandCatalog
    {
        private static readonly IReadOnlyDictionary<EditCommand, string> ScriptNames =
            new Dictionary<EditCommand, string>
            {
                [EditCommand.Cut] = BrowserScriptConstants.CutCommand,
                [EditCommand.Copy] = BrowserScriptConstants.CopyCommand,
                [EditCommand.Paste] = BrowserScriptConstants.PasteCommand,
                [EditCommand.SelectAll] = BrowserScriptConstants.SelectAllCommand
            };

        private static readonly IReadOnlyDictionary<
            EditCommand,
            Action<ComboBox, IClipboardService>> AddressBarActions =
            new Dictionary<EditCommand, Action<ComboBox, IClipboardService>>
            {
                [EditCommand.Cut] = (edit, clipboard) =>
                {
                    if (edit.SelectionLength > 0)
                    {
                        clipboard.SetText(edit.SelectedText);
                        edit.SelectedText = string.Empty;
                    }
                },
                [EditCommand.Copy] = (edit, clipboard) =>
                {
                    if (edit.SelectionLength > 0)
                    {
                        clipboard.SetText(edit.SelectedText);
                    }
                },
                [EditCommand.Paste] = (edit, clipboard) =>
                {
                    if (clipboard.ContainsText())
                    {
                        edit.SelectedText = clipboard.GetText();
                    }
                },
                [EditCommand.SelectAll] = (edit, clipboard) => edit.SelectAll()
            };

        internal static string GetScriptName(EditCommand command)
        {
            return ScriptNames[command];
        }

        internal static void ExecuteAddressBar(
            EditCommand command,
            ComboBox edit,
            IClipboardService clipboard)
        {
            AddressBarActions[command](edit, clipboard);
        }
    }
}
