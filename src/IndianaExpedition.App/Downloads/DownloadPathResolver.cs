using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Downloads
{
    internal static class DownloadPathResolver
    {
        internal static string Resolve(
            IWin32Window owner,
            BrowserSettings settings,
            string suggestedPath)
        {
            var directory = settings.DownloadDirectory;
            Directory.CreateDirectory(directory);
            var fileName = GetSuggestedFileName(suggestedPath);

            if (!settings.AskWhereToSaveDownloads)
            {
                return CreateUniquePath(directory, fileName);
            }

            using (var dialog = new SaveFileDialog
            {
                Title = Strings.DownloadSaveAsTitle,
                InitialDirectory = directory,
                FileName = fileName,
                Filter = Strings.AllFilesFilter,
                AddExtension = true,
                CheckPathExists = true,
                OverwritePrompt = true
            })
            {
                return dialog.ShowDialog(owner) == DialogResult.OK
                    ? dialog.FileName
                    : null;
            }
        }

        private static string GetSuggestedFileName(string suggestedPath)
        {
            var fileName = Path.GetFileName(suggestedPath);
            return string.IsNullOrWhiteSpace(fileName)
                ? BrowserUiConstants.DefaultDownloadFileName
                : fileName;
        }

        private static string CreateUniquePath(string directory, string fileName)
        {
            var candidate = Path.Combine(directory, fileName);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            for (var index = 1; index < BrowserUiConstants.MaximumDownloadNameAttempts; index++)
            {
                candidate = Path.Combine(
                    directory,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        BrowserUiConstants.UniqueDownloadNameFormat,
                        name,
                        index,
                        extension));
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(
                directory,
                Guid.NewGuid().ToString(BrowserUiConstants.UniqueIdentifierFormat) + extension);
        }
    }
}
