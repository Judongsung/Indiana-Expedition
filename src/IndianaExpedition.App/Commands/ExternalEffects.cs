using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace IndianaExpedition.Commands
{
    internal interface IExternalLauncher
    {
        void Open(string target);
    }

    internal sealed class ShellExternalLauncher : IExternalLauncher
    {
        public void Open(string target)
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
    }

    internal interface IClipboardService
    {
        bool ContainsText();
        string GetText();
        void SetText(string value);
    }

    internal sealed class WindowsClipboardService : IClipboardService
    {
        public bool ContainsText() => Clipboard.ContainsText();
        public string GetText() => Clipboard.GetText();
        public void SetText(string value) => Clipboard.SetText(value);
    }
}
