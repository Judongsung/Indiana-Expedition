using System;
using IndianaExpedition.Constants;

namespace IndianaExpedition
{
    internal enum VisualTestState
    {
        Main,
        Favorites,
        History
    }

    internal sealed class ApplicationLaunchOptions
    {
        private ApplicationLaunchOptions(
            bool isVisualTestMode,
            VisualTestState visualTestState,
            string visualTestDataDirectory,
            string visualTestReadyFile)
        {
            IsVisualTestMode = isVisualTestMode;
            VisualTestState = visualTestState;
            VisualTestDataDirectory = visualTestDataDirectory;
            VisualTestReadyFile = visualTestReadyFile;
        }

        internal bool IsVisualTestMode { get; }

        internal VisualTestState VisualTestState { get; }

        internal string VisualTestDataDirectory { get; }

        internal string VisualTestReadyFile { get; }

        internal static ApplicationLaunchOptions Parse(string[] arguments)
        {
            var visualTestMode = false;
            var visualTestState = VisualTestState.Main;
            string visualTestDataDirectory = null;
            string visualTestReadyFile = null;

            for (var index = 0; index < (arguments?.Length ?? 0); index++)
            {
                var argument = arguments[index];
                if (string.Equals(
                    argument,
                    ApplicationConstants.VisualTestModeArgument,
                    StringComparison.OrdinalIgnoreCase))
                {
                    visualTestMode = true;
                    continue;
                }

                if (TryReadOptionValue(
                    arguments,
                    ref index,
                    ApplicationConstants.VisualTestStateArgument,
                    out var stateValue) &&
                    Enum.TryParse(stateValue, ignoreCase: true, result: out VisualTestState parsedState))
                {
                    visualTestState = parsedState;
                    continue;
                }

                if (TryReadOptionValue(
                    arguments,
                    ref index,
                    ApplicationConstants.VisualTestDataDirectoryArgument,
                    out var dataDirectory))
                {
                    visualTestDataDirectory = dataDirectory;
                    continue;
                }

                if (TryReadOptionValue(
                    arguments,
                    ref index,
                    ApplicationConstants.VisualTestReadyFileArgument,
                    out var readyFile))
                {
                    visualTestReadyFile = readyFile;
                }
            }

            return new ApplicationLaunchOptions(
                visualTestMode,
                visualTestState,
                visualTestDataDirectory,
                visualTestReadyFile);
        }

        private static bool TryReadOptionValue(
            string[] arguments,
            ref int index,
            string option,
            out string value)
        {
            value = null;
            if (!string.Equals(arguments[index], option, StringComparison.OrdinalIgnoreCase) ||
                index + 1 >= arguments.Length ||
                string.IsNullOrWhiteSpace(arguments[index + 1]))
            {
                return false;
            }

            value = arguments[++index];
            return true;
        }
    }
}
