using System.Linq;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Navigation;

namespace IndianaExpedition.Core.Tests
{
    internal static class NavigationPolicyTests
    {
        internal static void Run()
        {
            var direct = AddressResolver.Resolve("https://example.com/path", BrowserDefaults.SearchUrlTemplate);
            TestAssert.Equal(AddressResolutionKind.Navigate, direct.Kind, "HTTPS URL은 직접 이동해야 합니다.");
            var search = AddressResolver.Resolve("윈도우 xp 브라우저", BrowserDefaults.SearchUrlTemplate);
            TestAssert.Equal(AddressResolutionKind.Search, search.Kind, "일반 문자열은 검색이어야 합니다.");
            var blocked = AddressResolver.Resolve("javascript:alert(1)", BrowserDefaults.SearchUrlTemplate);
            TestAssert.Equal(AddressResolutionKind.Blocked, blocked.Kind, "스크립트 URI를 차단해야 합니다.");

            TestAssert.True(
                PopupPolicy.TryNormalizeOrigin("https://Example.COM:443/path", out var origin),
                "출처를 정규화해야 합니다.");
            TestAssert.Equal("https://example.com", origin, "기본 포트와 대소문자를 정규화해야 합니다.");
            TestAssert.Equal(
                2,
                PopupPolicy.NormalizeOrigins(new[]
                {
                    "https://one.example/path",
                    "HTTPS://ONE.example/duplicate",
                    "http://one.example"
                }).Count,
                "스킴별 출처와 중복 정책이 잘못되었습니다.");
            TestAssert.True(
                !PopupPolicy.ShouldAllow(false, true, "https://blocked.example", Enumerable.Empty<string>()),
                "자동 팝업을 차단해야 합니다.");

            TestAssert.Equal(BrowserZoomLevel.Larger, BrowserZoomPolicy.Step(BrowserZoomLevel.Medium, 1), "확대 단계가 잘못되었습니다.");
            TestAssert.Equal(BrowserZoomLevel.Largest, BrowserZoomPolicy.Step(BrowserZoomLevel.Largest, 1), "확대 상한이 잘못되었습니다.");
            TestAssert.Equal(BrowserZoomLevel.Medium, BrowserZoomPolicy.Normalize((BrowserZoomLevel)999), "잘못된 확대값을 보정해야 합니다.");
        }
    }
}
