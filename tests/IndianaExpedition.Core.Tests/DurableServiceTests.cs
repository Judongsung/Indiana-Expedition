using System;
using System.IO;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition.Core.Tests
{
    internal static class DurableServiceTests
    {
        private const int DeepFavoriteTreeDepth = 3000;

        internal static void Run()
        {
            var favoriteStore = new MemoryDocumentStore<FavoritesDocument>(
                FavoritesDocument.CreateDefault(),
                value => value.DeepClone());
            var favorites = new FavoritesService(favoriteStore);
            var folder = favorites.AddFolder(null, "자료");
            var link = favorites.AddLink(folder.Id, "Microsoft", "https://www.microsoft.com/");
            favorites.Rename(link.Id, "Microsoft Home");
            TestAssert.Equal("Microsoft Home", favorites.Find(link.Id).Title, "즐겨찾기 변경이 반영되어야 합니다.");
            favoriteStore.FailWrites = true;
            var changed = 0;
            favorites.Changed += (sender, args) => changed++;
            TestAssert.Throws<IOException>(() => favorites.Delete(link.Id), "저장 실패를 전달해야 합니다.");
            TestAssert.True(favorites.Find(link.Id) != null, "저장 실패 시 항목을 메모리에서 지우면 안 됩니다.");
            TestAssert.Equal(0, changed, "실패한 변경 이벤트를 발행하면 안 됩니다.");
            VerifyDeepFavoriteTreeTraversal();

            var downloadStore = new MemoryDocumentStore<DownloadHistoryDocument>(
                DownloadHistoryDocument.CreateDefault(),
                value => value.DeepClone());
            var downloads = new DownloadHistoryService(downloadStore, 2);
            var record = downloads.Add(new DownloadRecord
            {
                FilePath = Path.Combine(Path.GetTempPath(), "download.zip"),
                FinishedAtUtc = DateTime.UtcNow,
                State = DownloadRecordState.Completed
            });
            downloadStore.FailWrites = true;
            TestAssert.Throws<IOException>(() => downloads.Remove(record.Id), "다운로드 기록 저장 실패를 전달해야 합니다.");
            TestAssert.Equal(1, downloads.Items.Count, "실패 시 다운로드 기록을 보존해야 합니다.");
        }

        private static void VerifyDeepFavoriteTreeTraversal()
        {
            var document = FavoritesDocument.CreateDefault();
            var root = FavoriteNode.CreateFolder("0");
            document.Items.Add(root);
            var current = root;
            for (var depth = 1; depth < DeepFavoriteTreeDepth; depth++)
            {
                var child = FavoriteNode.CreateFolder(depth.ToString());
                current.Children.Add(child);
                current = child;
            }

            var store = new MemoryDocumentStore<FavoritesDocument>(
                document,
                value => value.DeepClone());
            var service = new FavoritesService(store);
            TestAssert.Equal(
                current.Id,
                service.Find(current.Id).Id,
                "깊은 기존 즐겨찾기 트리도 재귀 스택에 의존하지 않고 탐색해야 합니다.");
        }
    }
}
