using IndianaExpedition.Commands;

namespace IndianaExpedition.VisualTestHost
{
    internal sealed class NoOpExternalLauncher : IExternalLauncher
    {
        internal static readonly NoOpExternalLauncher Instance = new NoOpExternalLauncher();

        private NoOpExternalLauncher()
        {
        }

        public void Open(string target)
        {
        }
    }

    internal sealed class InMemoryClipboardService : IClipboardService
    {
        private string _text;

        public bool ContainsText() => !string.IsNullOrEmpty(_text);
        public string GetText() => _text ?? string.Empty;
        public void SetText(string value) => _text = value;
    }
}
