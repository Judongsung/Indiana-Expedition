using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndianaExpedition.Commands;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Browser
{
    internal sealed partial class BrowserForm
    {
        private BrowserCommandCatalog CreateBrowserCommandCatalog()
        {
            return new BrowserCommandCatalog(new[]
            {
                Command(BrowserCommandId.NewWindow, () => Strings.NewWindow, () => _application.OpenWindow(), Keys.Control | Keys.N),
                Command(BrowserCommandId.OpenLocation, () => Strings.Open, ShowOpenLocationDialog, Keys.Control | Keys.O),
                Command(BrowserCommandId.CloseWindow, () => Strings.Close, Close, Keys.Alt | Keys.F4),
                Command(BrowserCommandId.Print, () => Strings.Print, PrintPage, Keys.Control | Keys.P),
                AsyncCommand(BrowserCommandId.Cut, () => Strings.Cut, () => ExecuteEditCommandAsync(EditCommand.Cut), Keys.Control | Keys.X),
                AsyncCommand(BrowserCommandId.Copy, () => Strings.Copy, () => ExecuteEditCommandAsync(EditCommand.Copy), Keys.Control | Keys.C),
                AsyncCommand(BrowserCommandId.Paste, () => Strings.Paste, () => ExecuteEditCommandAsync(EditCommand.Paste), Keys.Control | Keys.V),
                AsyncCommand(BrowserCommandId.SelectAll, () => Strings.SelectAll, () => ExecuteEditCommandAsync(EditCommand.SelectAll), Keys.Control | Keys.A),
                Command(BrowserCommandId.Find, () => Strings.Find, ShowPageFindDialog, Keys.Control | Keys.F),
                AsyncCommand(BrowserCommandId.FindNext, () => Strings.FindNext, () => RepeatPageFindAsync(false), Keys.F3),
                AsyncCommand(BrowserCommandId.FindPrevious, () => Strings.FindNext, () => RepeatPageFindAsync(true), Keys.Shift | Keys.F3),
                Command(BrowserCommandId.FocusAddress, () => Strings.AddressLabel, FocusAddressBar, Keys.Control | Keys.L),
                Command(BrowserCommandId.Back, () => Strings.Back, GoBack, () => CoreWebView?.CanGoBack == true, Keys.Alt | Keys.Left),
                Command(BrowserCommandId.Forward, () => Strings.Forward, GoForward, () => CoreWebView?.CanGoForward == true, Keys.Alt | Keys.Right),
                Command(BrowserCommandId.Stop, () => Strings.Stop, StopNavigation, () => _isLoading, Keys.Escape),
                Command(BrowserCommandId.Refresh, () => Strings.Refresh, RefreshPage, Keys.F5, Keys.Control | Keys.R),
                Command(BrowserCommandId.Home, () => Strings.Home, GoHome, Keys.Alt | Keys.Home),
                Command(BrowserCommandId.FavoritesSidebar, () => Strings.MenuFavorites, () => ToggleExplorerSidebar(ExplorerMode.Favorites), () => true, () => _sidebarController.IsSelected(ExplorerMode.Favorites), Keys.Control | Keys.I),
                Command(BrowserCommandId.HistorySidebar, () => Strings.HistoryTitle, () => ToggleExplorerSidebar(ExplorerMode.History), () => true, () => _sidebarController.IsSelected(ExplorerMode.History), Keys.Control | Keys.H),
                Command(BrowserCommandId.AddFavorite, () => Strings.AddFavorite, AddCurrentFavorite, Keys.Control | Keys.D),
                Command(BrowserCommandId.OrganizeFavorites, () => Strings.OrganizeFavorites, ShowOrganizeFavoritesDialog),
                Command(BrowserCommandId.Downloads, () => Strings.ViewDownloads, () => _application.Downloads.ShowHistory(this), Keys.Control | Keys.J),
                Command(BrowserCommandId.DeleteBrowsingData, () => Strings.DeleteHistory, ShowDeleteBrowsingDataDialog),
                Command(BrowserCommandId.PopupToggle, () => Strings.PopupBlockerEnabled, TogglePopupBlocker, () => true, () => _services.Settings.Current.PopupBlockerEnabled),
                Command(BrowserCommandId.PopupSettings, () => Strings.PopupBlockerSettings, ShowPopupBlockerSettingsDialog),
                Command(BrowserCommandId.InternetOptions, () => Strings.InternetOptions, ShowInternetOptionsDialog),
                Command(BrowserCommandId.About, () => Strings.About, ShowAboutDialog),
                Command(BrowserCommandId.FullScreen, () => Strings.FullScreen, ToggleFullScreen, Keys.F11),
                Command(BrowserCommandId.ZoomIn, () => Strings.TextSizeLarger, () => StepZoomLevel(1), Keys.Control | Keys.Add, Keys.Control | Keys.Oemplus, Keys.Control | Keys.Shift | Keys.Oemplus),
                Command(BrowserCommandId.ZoomOut, () => Strings.TextSizeSmaller, () => StepZoomLevel(-1), Keys.Control | Keys.Subtract, Keys.Control | Keys.OemMinus),
                Command(BrowserCommandId.ZoomReset, () => Strings.TextSizeMedium, () => SetZoomLevel(BrowserZoomLevel.Medium), Keys.Control | Keys.D0, Keys.Control | Keys.NumPad0)
            });
        }

        private static BrowserCommandDefinition Command(
            BrowserCommandId id,
            Func<string> text,
            Action execute,
            params Keys[] shortcuts)
        {
            return Command(id, text, execute, () => true, () => false, shortcuts);
        }

        private static BrowserCommandDefinition AsyncCommand(
            BrowserCommandId id,
            Func<string> text,
            Func<Task> executeAsync,
            params Keys[] shortcuts)
        {
            return new BrowserCommandDefinition(
                id,
                text,
                shortcuts,
                () => true,
                () => false,
                executeAsync);
        }

        private static BrowserCommandDefinition Command(
            BrowserCommandId id,
            Func<string> text,
            Action execute,
            Func<bool> canExecute,
            params Keys[] shortcuts)
        {
            return Command(id, text, execute, canExecute, () => false, shortcuts);
        }

        private static BrowserCommandDefinition Command(
            BrowserCommandId id,
            Func<string> text,
            Action execute,
            Func<bool> canExecute,
            Func<bool> isChecked,
            params Keys[] shortcuts)
        {
            return new BrowserCommandDefinition(
                id,
                text,
                shortcuts,
                canExecute,
                isChecked,
                () =>
                {
                    execute();
                    return Task.CompletedTask;
                });
        }

        private void TogglePopupBlocker()
        {
            _services.Settings.Update(settings => settings.PopupBlockerEnabled = !settings.PopupBlockerEnabled);
        }

        private void ShowCommandError(Exception exception)
        {
            MessageBox.Show(exception.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private ToolStripMenuItem CreateCommandMenuItem(BrowserCommandId id)
        {
            var definition = _commandCatalog.Get(id);
            var item = new ToolStripMenuItem(definition.GetText());
            if (definition.Shortcuts.Count > 0)
            {
                item.ShortcutKeyDisplayString =
                    new KeysConverter().ConvertToString(definition.Shortcuts[0]);
                item.ShowShortcutKeys = true;
            }
            item.Click += (sender, args) => _commandRouter.Execute(id);
            RegisterCommandItem(id, item);
            return item;
        }

        private void RegisterCommandItem(BrowserCommandId id, ToolStripItem item)
        {
            if (!_commandItems.TryGetValue(id, out var items))
            {
                items = new List<ToolStripItem>();
                _commandItems[id] = items;
            }
            items.Add(item);
        }

        private void RefreshCommandStates()
        {
            foreach (var pair in _commandItems)
            {
                var definition = _commandCatalog.Get(pair.Key);
                foreach (var item in pair.Value)
                {
                    item.Enabled = definition.CanExecute();
                    if (item is ToolStripMenuItem menuItem)
                    {
                        menuItem.Checked = definition.IsChecked();
                    }
                    else if (item is ToolStripButton button)
                    {
                        button.Checked = definition.IsChecked();
                    }
                }
            }
        }
    }
}
