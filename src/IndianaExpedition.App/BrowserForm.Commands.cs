using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Resources;

namespace IndianaExpedition
{
    internal sealed partial class BrowserForm
    {
        private void FocusAddressBar()
        {
            _addressBox.Focus();
            _addressBox.SelectAll();
        }

        private void NavigateFromAddressBar()
        {
            NavigateTo(_addressBox.Text);
        }

        private void NavigateTo(string input, bool allowExplicitFileUri = false)
        {
            var resolution = AddressResolver.Resolve(
                input,
                _services.Settings.Current.SearchUrlTemplate,
                allowExplicitFileUri);

            switch (resolution.Kind)
            {
                case AddressResolutionKind.Navigate:
                case AddressResolutionKind.Search:
                    NavigateResolvedTarget(resolution.Target);
                    break;
                case AddressResolutionKind.ExternalProtocol:
                    OpenExternalProtocol(resolution.Target);
                    break;
                default:
                    MessageBox.Show(
                        resolution.ErrorMessage,
                        Branding.ProductName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    break;
            }
        }

        private void NavigateResolvedTarget(string target)
        {
            if (!_browserReady || CoreWebView == null)
            {
                _addressBox.Text = target;
                return;
            }

            if (!_addressBox.Items.Contains(target))
            {
                _addressBox.Items.Insert(0, target);
            }
            CoreWebView.Navigate(target);
        }

        private void OpenExternalProtocol(string target)
        {
            var answer = MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, Strings.ExternalProtocolPromptFormat, target),
                Branding.ProductName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
        }

        private void GoBack()
        {
            if (CoreWebView?.CanGoBack == true)
            {
                CoreWebView.GoBack();
            }
        }

        private void GoForward()
        {
            if (CoreWebView?.CanGoForward == true)
            {
                CoreWebView.GoForward();
            }
        }

        private void StopNavigation()
        {
            if (_isLoading)
            {
                CoreWebView?.Stop();
            }
        }

        private void RefreshPage()
        {
            CoreWebView?.Reload();
        }

        private void GoHome()
        {
            NavigateTo(_services.Settings.Current.HomeUrl);
        }

        private void OnAddressKeyDown(object sender, KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Enter)
            {
                args.SuppressKeyPress = true;
                NavigateFromAddressBar();
            }
        }

        private void ShowOpenLocationDialog()
        {
            using (var dialog = new OpenLocationDialog(_addressBox.Text))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                NavigateTo(dialog.Target, dialog.IsLocalFile);
            }
        }

        private void AddCurrentFavorite()
        {
            var url = CoreWebView?.Source;
            if (string.IsNullOrWhiteSpace(url) ||
                string.Equals(url, BrowserDefaults.BlankPageUrl, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    Strings.NoPageToFavorite,
                    Branding.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var title = string.IsNullOrWhiteSpace(CoreWebView.DocumentTitle)
                ? url
                : CoreWebView.DocumentTitle;
            using (var dialog = new AddFavoriteDialog(_services.Favorites, title, url))
            {
                dialog.ShowDialog(this);
            }
        }

        private void ShowOrganizeFavoritesDialog()
        {
            using (var dialog = new OrganizeFavoritesDialog(_services.Favorites))
            {
                dialog.ShowDialog(this);
            }
        }

        private void ClearHistory()
        {
            var answer = MessageBox.Show(
                Strings.ClearHistoryConfirm,
                Branding.ProductName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
            {
                _services.History.Clear();
                _statusLabel.Text = Strings.ClearHistoryStatus;
            }
        }

        private void ShowInternetOptionsDialog()
        {
            using (var dialog = new InternetOptionsDialog(_services.Settings, _services.History, CoreWebView?.Source))
            {
                dialog.ShowDialog(this);
            }
        }

        private void ShowAboutDialog()
        {
            using (var dialog = new AboutDialog())
            {
                dialog.ShowDialog(this);
            }
        }

        private void ExecuteEditCommand(EditCommand command)
        {
            if (_addressBox.ContainsFocus)
            {
                var edit = _addressBox;
                switch (command)
                {
                    case EditCommand.Cut:
                        if (edit.SelectionLength > 0)
                        {
                            Clipboard.SetText(edit.SelectedText);
                            edit.SelectedText = string.Empty;
                        }
                        break;
                    case EditCommand.Copy:
                        if (edit.SelectionLength > 0)
                        {
                            Clipboard.SetText(edit.SelectedText);
                        }
                        break;
                    case EditCommand.Paste:
                        if (Clipboard.ContainsText())
                        {
                            edit.SelectedText = Clipboard.GetText();
                        }
                        break;
                    case EditCommand.SelectAll:
                        edit.SelectAll();
                        break;
                }
                return;
            }

            if (CoreWebView == null)
            {
                return;
            }

            string commandName;
            switch (command)
            {
                case EditCommand.Cut:
                    commandName = BrowserScriptConstants.CutCommand;
                    break;
                case EditCommand.Copy:
                    commandName = BrowserScriptConstants.CopyCommand;
                    break;
                case EditCommand.Paste:
                    commandName = BrowserScriptConstants.PasteCommand;
                    break;
                default:
                    commandName = BrowserScriptConstants.SelectAllCommand;
                    break;
            }

            _ = CoreWebView.ExecuteScriptAsync(string.Format(
                CultureInfo.InvariantCulture,
                BrowserScriptConstants.ExecuteCommandTemplate,
                commandName));
        }
    }
}
