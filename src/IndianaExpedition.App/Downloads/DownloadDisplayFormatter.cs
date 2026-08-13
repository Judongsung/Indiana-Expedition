using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Downloads
{
    internal static class DownloadDisplayFormatter
    {
        private static readonly IReadOnlyDictionary<DownloadTransferState, Func<string>>
            TransferStateTextByState =
                new ReadOnlyDictionary<DownloadTransferState, Func<string>>(
                    new Dictionary<DownloadTransferState, Func<string>>
                    {
                        [DownloadTransferState.InProgress] = () => Strings.DownloadStateInProgress,
                        [DownloadTransferState.Paused] = () => Strings.DownloadStatePaused,
                        [DownloadTransferState.Interrupted] = () => Strings.DownloadStateFailed,
                        [DownloadTransferState.Completed] = () => Strings.DownloadStateCompleted,
                        [DownloadTransferState.Canceled] = () => Strings.DownloadStateCanceled
                    });

        private static readonly IReadOnlyDictionary<DownloadRecordState, Func<string>>
            RecordStateTextByState =
                new ReadOnlyDictionary<DownloadRecordState, Func<string>>(
                    new Dictionary<DownloadRecordState, Func<string>>
                    {
                        [DownloadRecordState.Completed] = () => Strings.DownloadRecordCompleted,
                        [DownloadRecordState.Failed] = () => Strings.DownloadRecordFailed,
                        [DownloadRecordState.Canceled] = () => Strings.DownloadRecordCanceled
                    });

        private static readonly IReadOnlyDictionary<DownloadTransferState, Func<string, string>>
            BrowserStatusByState =
                new ReadOnlyDictionary<DownloadTransferState, Func<string, string>>(
                    new Dictionary<DownloadTransferState, Func<string, string>>
                    {
                        [DownloadTransferState.InProgress] = fileName => string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.DownloadInProgressFormat,
                            fileName),
                        [DownloadTransferState.Paused] = fileName => string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.DownloadPausedFormat,
                            fileName),
                        [DownloadTransferState.Interrupted] = fileName => string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.DownloadFailedFormat,
                            fileName),
                        [DownloadTransferState.Completed] = fileName => string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.DownloadCompletedFormat,
                            fileName),
                        [DownloadTransferState.Canceled] = fileName => string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.DownloadCanceledFormat,
                            fileName)
                    });

        private static readonly IReadOnlyList<Func<string>> ByteUnitText =
            new ReadOnlyCollection<Func<string>>(
                new List<Func<string>>
                {
                    () => Strings.ByteUnit,
                    () => Strings.KilobyteUnit,
                    () => Strings.MegabyteUnit,
                    () => Strings.GigabyteUnit
                });

        internal static string FormatState(DownloadTransferState state)
        {
            return TransferStateTextByState.TryGetValue(state, out var value)
                ? value()
                : Strings.DownloadStateFailed;
        }

        internal static string FormatState(DownloadRecordState state)
        {
            return RecordStateTextByState.TryGetValue(state, out var value)
                ? value()
                : Strings.DownloadStateFailed;
        }

        internal static string FormatBrowserStatus(DownloadTransferState state, string fileName)
        {
            return BrowserStatusByState.TryGetValue(state, out var value)
                ? value(fileName)
                : string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.DownloadFailedFormat,
                    fileName);
        }

        internal static string FormatBytes(long bytes)
        {
            var value = Math.Max(0L, bytes);
            var unitIndex = 0;
            var scaled = (double)value;
            while (scaled >= DownloadUiConstants.BytesPerUnit && unitIndex < ByteUnitText.Count - 1)
            {
                scaled /= DownloadUiConstants.BytesPerUnit;
                unitIndex++;
            }

            var format = unitIndex == 0
                ? DownloadUiConstants.WholeByteFormat
                : DownloadUiConstants.ScaledByteFormat;
            return string.Format(
                CultureInfo.CurrentCulture,
                format,
                scaled,
                ByteUnitText[unitIndex]());
        }
    }
}
