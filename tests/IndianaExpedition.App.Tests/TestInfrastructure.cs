using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using IndianaExpedition.Browser;
using IndianaExpedition.Constants;
using IndianaExpedition.Core;

namespace IndianaExpedition.App.Tests
{
    internal sealed class TestContext : IDisposable
    {
        private const string TemporaryDirectoryPrefix = "IndianaExpedition.App.Tests";
        private readonly ForegroundWindowGuard _foregroundGuard;
        private bool _disposed;

        internal TestContext(ForegroundWindowGuard foregroundGuard)
        {
            _foregroundGuard = foregroundGuard ?? throw new ArgumentNullException(nameof(foregroundGuard));
            TemporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                TemporaryDirectoryPrefix,
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(TemporaryDirectory);
            _foregroundGuard.ThrowIfViolated();
        }

        internal string TemporaryDirectory { get; }

        internal void PumpEvents()
        {
            for (var index = 0; index < TestConstants.EventPumpPasses; index++)
            {
                Application.DoEvents();
                Thread.Yield();
                _foregroundGuard.ThrowIfViolated();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            PumpEvents();
            if (Directory.Exists(TemporaryDirectory))
            {
                Directory.Delete(TemporaryDirectory, recursive: true);
            }
            _disposed = true;
        }
    }

    internal sealed class BrowserTestHost : IDisposable
    {
        private readonly TestContext _testContext;
        private readonly BrowserApplicationContext _applicationContext;
        private bool _disposed;

        internal BrowserTestHost(TestContext testContext, VisualTestState visualTestState)
        {
            _testContext = testContext ?? throw new ArgumentNullException(nameof(testContext));
            Services = new BrowserApplicationServices(new AppDataPaths(testContext.TemporaryDirectory));
            var launchOptions = ApplicationLaunchOptions.Parse(new[]
            {
                ApplicationConstants.VisualTestModeArgument,
                ApplicationConstants.VisualTestStateArgument,
                visualTestState.ToString(),
                ApplicationConstants.VisualTestDataDirectoryArgument,
                testContext.TemporaryDirectory
            });
            _applicationContext = new BrowserApplicationContext(Services, launchOptions);
            _testContext.PumpEvents();
            Browser = Application.OpenForms
                .Cast<Form>()
                .OfType<BrowserForm>()
                .Single();
        }

        internal BrowserApplicationServices Services { get; }

        internal BrowserForm Browser { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _applicationContext.Dispose();
            _testContext.PumpEvents();
            _disposed = true;
        }
    }

    internal sealed class ForegroundWindowGuard : IDisposable
    {
        private const uint EventSystemForeground = 0x0003;
        private const uint WinEventOutOfContext = 0x0000;
        private readonly uint _processId = unchecked((uint)Process.GetCurrentProcess().Id);
        private readonly WinEventDelegate _callback;
        private readonly IntPtr _hook;
        private long _violatingWindow;
        private string _violationDescription;

        internal ForegroundWindowGuard()
        {
            _callback = OnWinEvent;
            _hook = SetWinEventHook(
                EventSystemForeground,
                EventSystemForeground,
                IntPtr.Zero,
                _callback,
                0,
                0,
                WinEventOutOfContext);
            if (_hook == IntPtr.Zero)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "전경 창 변경 감시 훅을 설치하지 못했습니다.");
            }
            Observe(GetForegroundWindow());
            ThrowIfViolated();
        }

        internal void ThrowIfViolated()
        {
            Observe(GetForegroundWindow());
            var window = Interlocked.Read(ref _violatingWindow);
            if (window != 0)
            {
                throw new InvalidOperationException(
                    "테스트 프로세스가 전경 창을 획득했습니다. HWND=" + window +
                    ", " + (_violationDescription ?? "창 정보 없음"));
            }
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
            }
        }

        private void OnWinEvent(
            IntPtr hook,
            uint eventType,
            IntPtr window,
            int objectId,
            int childId,
            uint eventThread,
            uint eventTime)
        {
            Observe(window);
        }

        private void Observe(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return;
            }

            GetWindowThreadProcessId(window, out var ownerProcessId);
            if (ownerProcessId == _processId)
            {
                if (Interlocked.CompareExchange(ref _violatingWindow, window.ToInt64(), 0) == 0)
                {
                    _violationDescription = ReadWindowDescription(window);
                }
            }
        }

        private static string ReadWindowDescription(IntPtr window)
        {
            var title = new StringBuilder(TestConstants.WindowTextCapacity);
            var className = new StringBuilder(TestConstants.WindowTextCapacity);
            GetWindowText(window, title, title.Capacity);
            GetClassName(window, className, className.Capacity);
            return "Title='" + title + "', Class='" + className + "'";
        }

        private delegate void WinEventDelegate(
            IntPtr hook,
            uint eventType,
            IntPtr window,
            int objectId,
            int childId,
            uint eventThread,
            uint eventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximumCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window, StringBuilder className, int maximumCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr eventHookModule,
            WinEventDelegate callback,
            uint processId,
            uint threadId,
            uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWinEvent(IntPtr hook);
    }

    internal static class ControlLookup
    {
        internal static T RequireControl<T>(Control root, string name) where T : Control
        {
            var matches = root.Controls.Find(name, searchAllChildren: true);
            if (matches.Length != 1 || !(matches[0] is T typed))
            {
                throw new InvalidOperationException(
                    "UI 자동화 ID에 해당하는 컨트롤이 정확히 하나가 아닙니다: " + name);
            }
            return typed;
        }

        internal static T RequireToolStripItem<T>(Control root, string name) where T : ToolStripItem
        {
            var matches = EnumerateControls(root)
                .OfType<ToolStrip>()
                .SelectMany(toolStrip => EnumerateItems(toolStrip.Items))
                .Where(item => string.Equals(item.Name, name, StringComparison.Ordinal))
                .OfType<T>()
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "UI 자동화 ID에 해당하는 도구 모음 항목이 정확히 하나가 아닙니다: " + name);
            }
            return matches[0];
        }

        private static IEnumerable<Control> EnumerateControls(Control root)
        {
            yield return root;
            foreach (Control child in root.Controls)
            {
                foreach (var descendant in EnumerateControls(child))
                {
                    yield return descendant;
                }
            }
        }

        private static IEnumerable<ToolStripItem> EnumerateItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                yield return item;
                if (item is ToolStripDropDownItem dropDownItem)
                {
                    foreach (var child in EnumerateItems(dropDownItem.DropDownItems))
                    {
                        yield return child;
                    }
                }
            }
        }
    }

    internal static class TestAssert
    {
        internal static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        internal static void False(bool condition, string message)
        {
            True(!condition, message);
        }

        internal static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    message + " Expected: " + expected + ", Actual: " + actual);
            }
        }

        internal static void SequenceEqual<T>(
            IEnumerable<T> expected,
            IEnumerable<T> actual,
            string message)
        {
            if (!expected.SequenceEqual(actual))
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    internal static class TestConstants
    {
        internal const int EventPumpPasses = 3;
        internal const int WindowTextCapacity = 512;
    }
}
