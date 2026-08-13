using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;
using IndianaExpedition.Core.Persistence;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition.Core.Tests
{
    internal static class Program
    {
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            Run("주소 해석", AddressResolutionTests);
            Run("설정 저장과 손상 복구", SettingsTests);
            Run("팝업 출처와 차단 정책", PopupPolicyTests);
            Run("브라우저 확대 단계", BrowserZoomPolicyTests);
            Run("중첩 즐겨찾기 CRUD", FavoritesTests);
            Run("방문 기록 보관 정책", HistoryTests);
            Run("마지막 세션 저장", SessionTests);

            if (Failures.Count == 0)
            {
                Console.WriteLine("PASS: IndianaExpedition.Core 테스트가 모두 통과했습니다.");
                return 0;
            }

            Console.Error.WriteLine("FAIL: " + Failures.Count + "개 테스트가 실패했습니다.");
            foreach (var failure in Failures)
            {
                Console.Error.WriteLine(" - " + failure);
            }

            return 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception ex)
            {
                Failures.Add(name + ": " + ex.Message);
            }
        }

        private static void AddressResolutionTests()
        {
            var direct = AddressResolver.Resolve(
                "https://example.com/path",
                BrowserDefaults.SearchUrlTemplate);
            Equal(AddressResolutionKind.Navigate, direct.Kind, "HTTPS URL은 직접 이동해야 합니다.");

            var inferred = AddressResolver.Resolve(
                "example.com/path",
                BrowserDefaults.SearchUrlTemplate);
            Equal("https://example.com/path", inferred.Target.TrimEnd('/'), "호스트에 HTTPS를 보완해야 합니다.");

            var localhost = AddressResolver.Resolve(
                "localhost:5000",
                BrowserDefaults.SearchUrlTemplate);
            Equal(AddressResolutionKind.Navigate, localhost.Kind, "localhost와 포트는 직접 이동해야 합니다.");
            Equal("https://localhost:5000/", localhost.Target, "localhost 포트 주소를 올바르게 보완해야 합니다.");

            var search = AddressResolver.Resolve(
                "윈도우 xp 인터넷 익스플로러",
                BrowserDefaults.SearchUrlTemplate);
            Equal(AddressResolutionKind.Search, search.Kind, "일반 문자열은 검색이어야 합니다.");
            True(search.Target.Contains("%EC%9C%88%EB%8F%84%EC%9A%B0"), "검색어는 URL 인코딩해야 합니다.");

            var blocked = AddressResolver.Resolve(
                "javascript:alert(1)",
                BrowserDefaults.SearchUrlTemplate);
            Equal(AddressResolutionKind.Blocked, blocked.Kind, "javascript: URI를 차단해야 합니다.");

            var external = AddressResolver.Resolve(
                "mailto:test@example.com",
                BrowserDefaults.SearchUrlTemplate);
            Equal(AddressResolutionKind.ExternalProtocol, external.Kind, "외부 프로토콜을 분리해야 합니다.");
        }

        private static void SettingsTests()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "settings.json");
                var service = new SettingsService(path);
                Equal(BrowserDefaults.HomeUrl, service.Current.HomeUrl, "기본 홈 URL이 달라졌습니다.");

                service.Update(settings =>
                {
                    settings.HomeUrl = "https://example.com/";
                    settings.StartupMode = StartupMode.LastActivePage;
                });

                var reloaded = new SettingsService(path);
                Equal("https://example.com/", reloaded.Current.HomeUrl, "홈 URL이 유지되지 않았습니다.");
                Equal(StartupMode.LastActivePage, reloaded.Current.StartupMode, "시작 모드가 유지되지 않았습니다.");

                File.WriteAllText(
                    path,
                    "{\"SchemaVersion\":1,\"UiCulture\":\"ko-KR\",\"HomeUrl\":\"https://legacy.example/\",\"SearchUrlTemplate\":\"https://www.google.com/search?q={query}\",\"StartupMode\":1,\"DownloadDirectory\":\"C:\\\\Downloads\",\"ShowLinksBar\":true,\"ShowStatusBar\":true}");
                var migrated = new SettingsService(path);
                Equal(BrowserDefaults.DataSchemaVersion, migrated.Current.SchemaVersion, "설정 스키마가 이관되지 않았습니다.");
                Equal("ko-KR", migrated.Current.UiCulture, "이관 중 UI 문화권이 손실되었습니다.");
                Equal("https://legacy.example/", migrated.Current.HomeUrl, "이관 중 기존 설정이 손실되었습니다.");
                Equal(BrowserDefaults.SearchUrlTemplate, migrated.Current.SearchUrlTemplate, "이관 중 검색 주소가 손실되었습니다.");
                Equal(StartupMode.LastActivePage, migrated.Current.StartupMode, "이관 중 시작 방식이 손실되었습니다.");
                Equal(Path.GetFullPath("C:\\Downloads"), migrated.Current.DownloadDirectory, "이관 중 다운로드 폴더가 손실되었습니다.");
                True(migrated.Current.ShowLinksBar, "이관 중 연결 표시줄 설정이 손실되었습니다.");
                True(migrated.Current.ShowStatusBar, "이관 중 상태 표시줄 설정이 손실되었습니다.");
                True(migrated.Current.PopupBlockerEnabled, "기존 설정 이관 시 팝업 차단이 기본 활성화되어야 합니다.");
                Equal(BrowserZoomLevel.Medium, migrated.Current.DefaultZoomLevel, "기존 설정 이관 시 확대 수준이 100%여야 합니다.");
                var persistedMigration = new AtomicJsonFileStore<BrowserSettings>(path, BrowserSettings.CreateDefault).Load();
                Equal(BrowserDefaults.DataSchemaVersion, persistedMigration.SchemaVersion, "이관된 설정 스키마가 파일에 저장되지 않았습니다.");

                migrated.Update(settings =>
                {
                    settings.PopupBlockerEnabled = false;
                    settings.DefaultZoomLevel = BrowserZoomLevel.Largest;
                    settings.AllowedPopupOrigins.Add("HTTPS://Example.COM/path");
                    settings.AllowedPopupOrigins.Add("https://example.com/duplicate");
                    settings.AllowedPopupOrigins.Add("javascript:alert(1)");
                });
                var migratedReloaded = new SettingsService(path);
                True(!migratedReloaded.Current.PopupBlockerEnabled, "사용자가 끈 팝업 차단 설정이 유지되지 않았습니다.");
                Equal(BrowserZoomLevel.Largest, migratedReloaded.Current.DefaultZoomLevel, "확대 수준이 유지되지 않았습니다.");
                Equal(1, migratedReloaded.Current.AllowedPopupOrigins.Count, "허용 출처 정규화 또는 중복 제거가 잘못되었습니다.");
                Equal("https://example.com", migratedReloaded.Current.AllowedPopupOrigins[0], "허용 출처가 정규화되지 않았습니다.");

                migratedReloaded.Update(settings => settings.DefaultZoomLevel = (BrowserZoomLevel)999);
                Equal(BrowserZoomLevel.Medium, migratedReloaded.Current.DefaultZoomLevel, "잘못된 확대 수준을 기본값으로 복구하지 못했습니다.");

                File.WriteAllText(path, "{ invalid json");
                var recovered = new SettingsService(path);
                Equal(BrowserDefaults.HomeUrl, recovered.Current.HomeUrl, "손상 파일에서 기본값으로 복구하지 못했습니다.");
                True(Directory.GetFiles(root, "settings.json.corrupt-*.bak").Length == 1, "손상 파일 백업이 없습니다.");
            });
        }

        private static void PopupPolicyTests()
        {
            True(
                PopupPolicy.TryNormalizeOrigin("https://Example.COM:443/path?q=1", out var httpsOrigin),
                "HTTPS 출처를 정규화하지 못했습니다.");
            Equal("https://example.com", httpsOrigin, "기본 HTTPS 포트가 제거되지 않았습니다.");

            True(
                PopupPolicy.TryNormalizeOrigin("http://example.com:8080/path", out var portOrigin),
                "포트가 있는 출처를 정규화하지 못했습니다.");
            Equal("http://example.com:8080", portOrigin, "비기본 포트가 유지되지 않았습니다.");
            True(!PopupPolicy.TryNormalizeOrigin("file:///C:/test.html", out _), "HTTP(S)가 아닌 출처를 허용하면 안 됩니다.");

            var allowed = new[] { "https://allowed.example", "https://duplicate.example/path", "https://DUPLICATE.example/other" };
            Equal(2, PopupPolicy.NormalizeOrigins(allowed).Count, "허용 출처 중복 제거가 잘못되었습니다.");
            var overLimit = Enumerable.Range(0, PopupPolicyConstants.MaximumAllowedOrigins + 10)
                .Select(index => "https://site" + index + ".example");
            Equal(
                PopupPolicyConstants.MaximumAllowedOrigins,
                PopupPolicy.NormalizeOrigins(overLimit).Count,
                "허용 출처 최대 개수가 적용되지 않았습니다.");
            True(PopupPolicy.ShouldAllow(true, true, "https://blocked.example", allowed), "사용자 시작 팝업은 허용해야 합니다.");
            True(PopupPolicy.ShouldAllow(false, false, "https://blocked.example", allowed), "차단기가 꺼지면 팝업을 허용해야 합니다.");
            True(PopupPolicy.ShouldAllow(false, true, "https://allowed.example/page", allowed), "허용 출처의 팝업을 차단하면 안 됩니다.");
            True(!PopupPolicy.ShouldAllow(false, true, "https://sub.allowed.example", allowed), "하위 도메인을 자동으로 허용하면 안 됩니다.");
            True(!PopupPolicy.ShouldAllow(false, true, "http://allowed.example", allowed), "다른 스킴을 자동으로 허용하면 안 됩니다.");
            True(!PopupPolicy.ShouldAllow(false, true, "https://allowed.example:444", allowed), "다른 포트를 자동으로 허용하면 안 됩니다.");
        }

        private static void BrowserZoomPolicyTests()
        {
            Equal(BrowserZoomLevel.Larger, BrowserZoomPolicy.Step(BrowserZoomLevel.Medium, 1), "확대 단계가 증가하지 않았습니다.");
            Equal(BrowserZoomLevel.Smaller, BrowserZoomPolicy.Step(BrowserZoomLevel.Medium, -1), "확대 단계가 감소하지 않았습니다.");
            Equal(BrowserZoomLevel.Largest, BrowserZoomPolicy.Step(BrowserZoomLevel.Largest, 1), "최대 확대 단계를 넘으면 안 됩니다.");
            Equal(BrowserZoomLevel.Smallest, BrowserZoomPolicy.Step(BrowserZoomLevel.Smallest, -1), "최소 확대 단계를 넘으면 안 됩니다.");
            Equal(BrowserZoomLevel.Larger, BrowserZoomPolicy.Step((BrowserZoomLevel)999, 1), "잘못된 확대 단계는 중간값에서 보정해야 합니다.");
            Equal(BrowserZoomLevel.Medium, BrowserZoomPolicy.Normalize((BrowserZoomLevel)999), "잘못된 확대 단계의 정규화가 잘못되었습니다.");
        }

        private static void FavoritesTests()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "favorites.json");
                var service = new FavoritesService(path);
                var parent = service.AddFolder(null, "자료");
                var child = service.AddFolder(parent.Id, "WebView2");
                var link = service.AddLink(parent.Id, "Microsoft", "https://www.microsoft.com/");

                service.Move(link.Id, child.Id);
                service.Rename(link.Id, "Microsoft Home");

                var reloaded = new FavoritesService(path);
                var loadedParent = reloaded.Find(parent.Id);
                Equal(1, loadedParent.Children.Count, "최상위 폴더 구조가 잘못되었습니다.");
                Equal("Microsoft Home", reloaded.Find(link.Id).Title, "변경된 이름이 유지되지 않았습니다.");

                Throws<InvalidOperationException>(
                    () => reloaded.Move(parent.Id, child.Id),
                    "폴더를 자신의 하위 폴더로 이동하지 못하게 해야 합니다.");

                reloaded.Delete(child.Id);
                True(reloaded.Find(link.Id) == null, "폴더 삭제 시 하위 항목도 삭제되어야 합니다.");
            });
        }

        private static void HistoryTests()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "history.json");
                var now = DateTime.UtcNow;
                var service = new HistoryService(path, retentionDays: 30, maximumEntries: 2);

                True(!service.RecordNavigation(BrowserDefaults.BlankPageUrl, "Blank", now), "내부 페이지는 기록하지 않아야 합니다.");
                service.RecordNavigation("https://one.example/", "One", now.AddMinutes(-2));
                service.RecordNavigation("https://one.example/", "One updated", now.AddMinutes(-1));
                Equal(1, service.Items.Count, "연속 중복 방문을 합쳐야 합니다.");

                service.RecordNavigation("https://two.example/", "Two", now);
                service.RecordNavigation("https://three.example/", "Three", now.AddMinutes(1));
                Equal(2, service.Items.Count, "최대 항목 수를 적용해야 합니다.");
                Equal("https://three.example/", service.Items[0].Url, "최신 방문 순서가 잘못되었습니다.");

                service.Clear();
                Equal(0, new HistoryService(path).Items.Count, "기록 삭제가 저장되지 않았습니다.");
            });
        }

        private static void SessionTests()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "session.json");
                var service = new SessionService(path);
                service.Remember("ftp://example.com/file");
                True(string.IsNullOrEmpty(service.Current.LastActiveUrl), "지원하지 않는 URL을 세션에 저장하면 안 됩니다.");

                service.Remember("https://example.com/");
                var reloaded = new SessionService(path);
                Equal("https://example.com/", reloaded.Current.LastActiveUrl, "마지막 URL이 유지되지 않았습니다.");
            });
        }

        private static void WithTemporaryDirectory(Action<string> test)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "indiana-expedition-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                test(root);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message + " Expected: " + expected + ", Actual: " + actual);
            }
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Throws<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
