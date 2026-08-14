using System;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Styling;

namespace IndianaExpedition.App.Tests
{
    internal static class BrowserInteractionTests
    {
        internal static void ExplorerSidebarToggle(TestContext context)
        {
            using (var host = new BrowserTestHost(context, VisualTestState.Main))
            {
                var favorites = ControlLookup.RequireToolStripItem<ToolStripButton>(
                    host.Browser,
                    UiAutomationIds.Browser.FavoritesSidebarButton);
                var history = ControlLookup.RequireToolStripItem<ToolStripButton>(
                    host.Browser,
                    UiAutomationIds.Browser.HistorySidebarButton);
                var split = ControlLookup.RequireControl<SplitContainer>(
                    host.Browser,
                    UiAutomationIds.Browser.ContentSplit);

                AssertSidebarState(split, favorites, history, collapsed: true, favoritesChecked: false, historyChecked: false);

                favorites.PerformClick();
                context.PumpEvents();
                AssertSidebarState(split, favorites, history, collapsed: false, favoritesChecked: true, historyChecked: false);

                favorites.PerformClick();
                context.PumpEvents();
                AssertSidebarState(split, favorites, history, collapsed: true, favoritesChecked: false, historyChecked: false);

                favorites.PerformClick();
                history.PerformClick();
                context.PumpEvents();
                AssertSidebarState(split, favorites, history, collapsed: false, favoritesChecked: false, historyChecked: true);

                history.PerformClick();
                context.PumpEvents();
                AssertSidebarState(split, favorites, history, collapsed: true, favoritesChecked: false, historyChecked: false);
            }
        }

        internal static void TopLevelMenuToggle(TestContext context)
        {
            using (var host = new BrowserTestHost(context, VisualTestState.Main))
            {
                var menu = ControlLookup.RequireControl<XpMenuStrip>(
                    host.Browser,
                    UiAutomationIds.Browser.MainMenu);
                var favorites = ControlLookup.RequireToolStripItem<ToolStripMenuItem>(
                    host.Browser,
                    UiAutomationIds.Browser.FavoritesMenu);
                var help = ControlLookup.RequireToolStripItem<ToolStripMenuItem>(
                    host.Browser,
                    UiAutomationIds.Browser.HelpMenu);

                AssertTopLevelMenuToggle(menu, favorites, context);
                AssertTopLevelMenuToggle(menu, help, context);
            }
        }

        internal static void PopupInformationBarClose(TestContext context)
        {
            using (var host = new BrowserTestHost(context, VisualTestState.PopupBlocked))
            {
                var informationBar = ControlLookup.RequireControl<Panel>(
                    host.Browser,
                    UiAutomationIds.Browser.InformationBar);
                var openButton = ControlLookup.RequireControl<Button>(
                    host.Browser,
                    UiAutomationIds.Browser.OpenBlockedPopupButton);
                var allowButton = ControlLookup.RequireControl<Button>(
                    host.Browser,
                    UiAutomationIds.Browser.AllowPopupOriginButton);
                var closeButton = ControlLookup.RequireControl<Button>(
                    host.Browser,
                    UiAutomationIds.Browser.CloseInformationBarButton);

                TestAssert.True(informationBar.Visible, "차단된 팝업 정보 표시줄이 표시되지 않았습니다.");
                closeButton.PerformClick();
                context.PumpEvents();

                TestAssert.False(informationBar.Visible, "닫기 후 팝업 정보 표시줄이 숨겨지지 않았습니다.");
                TestAssert.False(openButton.Enabled, "닫기 후 보류 팝업 열기 상태가 남았습니다.");
                TestAssert.False(allowButton.Enabled, "닫기 후 보류 출처 허용 상태가 남았습니다.");
            }
        }

        internal static void PopupOriginAllow(TestContext context)
        {
            using (var host = new BrowserTestHost(context, VisualTestState.PopupBlocked))
            {
                var informationBar = ControlLookup.RequireControl<Panel>(
                    host.Browser,
                    UiAutomationIds.Browser.InformationBar);
                var openButton = ControlLookup.RequireControl<Button>(
                    host.Browser,
                    UiAutomationIds.Browser.OpenBlockedPopupButton);
                var allowButton = ControlLookup.RequireControl<Button>(
                    host.Browser,
                    UiAutomationIds.Browser.AllowPopupOriginButton);

                allowButton.PerformClick();
                context.PumpEvents();

                var settings = host.Services.Settings.Current;
                TestAssert.Equal(
                    1,
                    settings.AllowedPopupOrigins.Count(
                        origin => string.Equals(
                            origin,
                            VisualTestConstants.PopupSourceOrigin,
                            StringComparison.OrdinalIgnoreCase)),
                    "팝업 출처가 허용 목록에 정확히 한 번 저장되지 않았습니다.");
                TestAssert.False(informationBar.Visible, "허용한 출처의 보류 팝업이 정보 표시줄에 남았습니다.");
                TestAssert.False(openButton.Enabled, "허용한 출처의 보류 대상이 남았습니다.");
                TestAssert.False(allowButton.Enabled, "허용한 출처가 보류 목록에 남았습니다.");
            }
        }

        private static void AssertSidebarState(
            SplitContainer split,
            ToolStripButton favorites,
            ToolStripButton history,
            bool collapsed,
            bool favoritesChecked,
            bool historyChecked)
        {
            TestAssert.Equal(collapsed, split.Panel1Collapsed, "사이드바 표시 상태가 잘못되었습니다.");
            TestAssert.Equal(favoritesChecked, favorites.Checked, "즐겨찾기 버튼 눌림 상태가 잘못되었습니다.");
            TestAssert.Equal(historyChecked, history.Checked, "기록 버튼 눌림 상태가 잘못되었습니다.");
        }

        private static void AssertTopLevelMenuToggle(
            XpMenuStrip menu,
            ToolStripMenuItem item,
            TestContext context)
        {
            item.ShowDropDown();
            context.PumpEvents();
            TestAssert.True(item.DropDown.Visible, "상위 메뉴 드롭다운이 열리지 않았습니다.");
            TestAssert.True(item.Pressed, "열린 상위 메뉴가 눌린 상태로 표시되지 않았습니다.");

            TestAssert.True(
                menu.TryCloseOpenDropDown(item, MouseButtons.Left),
                "같은 상위 메뉴를 다시 누르는 경로가 드롭다운을 처리하지 않았습니다.");
            context.PumpEvents();
            TestAssert.False(item.DropDown.Visible, "같은 상위 메뉴 재클릭 후 드롭다운이 닫히지 않았습니다.");
            TestAssert.False(item.Pressed, "닫힌 상위 메뉴의 눌림 상태가 해제되지 않았습니다.");
        }
    }
}
