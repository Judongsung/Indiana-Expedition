using IndianaExpedition.Constants;
using IndianaExpedition.Resources;

namespace IndianaExpedition
{
    internal static class Branding
    {
        // Product-facing values live behind this class so a later brand swap is localized.
        internal static string ProductName => Strings.ProductName;

        internal static string FormatWindowTitle(string documentTitle)
        {
            return string.IsNullOrWhiteSpace(documentTitle)
                ? ProductName
                : documentTitle.Trim() + ApplicationConstants.WindowTitleSeparator + ProductName;
        }
    }
}
