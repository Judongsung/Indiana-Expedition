using System.IO;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Persistence;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition.Core.Tests
{
    internal static class SettingsTests
    {
        internal static void Run()
        {
            TestAssert.WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, StorageConstants.SettingsFileName);
                File.WriteAllText(
                    path,
                    "{\"SchemaVersion\":1,\"UiCulture\":\"ko-KR\",\"HomeUrl\":\"https://legacy.example/\",\"SearchUrlTemplate\":\"https://www.google.com/search?q={query}\",\"StartupMode\":1,\"DownloadDirectory\":\"C:\\\\Downloads\",\"ShowLinksBar\":true,\"ShowStatusBar\":true}");
                var service = new SettingsService(path);
                TestAssert.Equal(BrowserDefaults.DataSchemaVersion, service.Current.SchemaVersion, "스키마 1을 3으로 이관해야 합니다.");
                TestAssert.Equal("https://legacy.example/", service.Current.HomeUrl, "기존 값을 보존해야 합니다.");
                TestAssert.True(service.Current.PopupBlockerEnabled, "이전 스키마의 팝업 차단 기본값이 잘못되었습니다.");

                service.Update(settings =>
                {
                    settings.PopupBlockerEnabled = false;
                    settings.AllowedPopupOrigins.Add("HTTPS://Example.COM/path");
                    settings.AllowedPopupOrigins.Add("https://example.com/again");
                });
                var reloaded = new SettingsService(path);
                TestAssert.True(!reloaded.Current.PopupBlockerEnabled, "사용자가 끈 값을 보존해야 합니다.");
                TestAssert.Equal(1, reloaded.Current.AllowedPopupOrigins.Count, "출처 중복을 제거해야 합니다.");

                var failingStore = new MemoryDocumentStore<BrowserSettings>(
                    BrowserSettings.CreateDefault(),
                    value => value.Clone());
                var transactional = new SettingsService(failingStore, BrowserSettings.CreateDefault);
                failingStore.FailWrites = true;
                var changed = 0;
                transactional.Changed += (sender, args) => changed++;
                TestAssert.Throws<IOException>(
                    () => transactional.Update(value => value.HomeUrl = "https://not-saved.example/"),
                    "저장 장애가 호출자에게 전달되어야 합니다.");
                TestAssert.Equal(BrowserDefaults.HomeUrl, transactional.Current.HomeUrl, "실패 시 메모리 상태가 바뀌면 안 됩니다.");
                TestAssert.Equal(0, changed, "실패 시 Changed를 발행하면 안 됩니다.");

                var invalidSettings = BrowserSettings.CreateDefault();
                invalidSettings.HomeUrl = "not-a-url";
                invalidSettings.SearchUrlTemplate = "missing-query-token";
                invalidSettings.DownloadDirectory = "\0";
                invalidSettings.StartupMode = (StartupMode)999;
                var normalizationStore = new MemoryDocumentStore<BrowserSettings>(
                    invalidSettings,
                    value => value.Clone());
                var normalizedService = new SettingsService(
                    normalizationStore,
                    BrowserSettings.CreateDefault);
                TestAssert.Equal(
                    1,
                    normalizationStore.SaveCount,
                    "여러 설정 필드가 정규화되어도 시작 저장은 한 번만 수행해야 합니다.");
                TestAssert.Equal(
                    BrowserDefaults.HomeUrl,
                    normalizedService.Current.HomeUrl,
                    "잘못된 홈 주소를 기본값으로 정규화해야 합니다.");

                File.WriteAllText(path, "{ invalid json");
                var recovered = new SettingsService(path);
                TestAssert.Equal(BrowserDefaults.HomeUrl, recovered.Current.HomeUrl, "손상 파일을 복구해야 합니다.");
            });
        }
    }
}
