using System;
using System.Collections.Generic;

namespace IndianaExpedition.Core.Tests
{
    internal static class Program
    {
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            Run("주소와 정책", NavigationPolicyTests.Run);
            Run("설정 저장과 이관", SettingsTests.Run);
            Run("즐겨찾기와 다운로드 기록", DurableServiceTests.Run);
            Run("지연 저장과 방문 기록", DeferredPersistenceTests.Run);

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
                Failures.Add(name + Environment.NewLine + ex);
            }
        }
    }
}
