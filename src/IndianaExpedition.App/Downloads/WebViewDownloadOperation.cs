using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.Downloads
{
    internal sealed class WebViewDownloadOperation : IDownloadOperation
    {
        private static readonly IReadOnlyDictionary<CoreWebView2DownloadState, Func<WebViewDownloadOperation, DownloadTransferState>>
            StateByWebViewState =
                new ReadOnlyDictionary<CoreWebView2DownloadState, Func<WebViewDownloadOperation, DownloadTransferState>>(
                    new Dictionary<CoreWebView2DownloadState, Func<WebViewDownloadOperation, DownloadTransferState>>
                    {
                        [CoreWebView2DownloadState.InProgress] = operation => DownloadTransferState.InProgress,
                        [CoreWebView2DownloadState.Completed] = operation => DownloadTransferState.Completed,
                        [CoreWebView2DownloadState.Interrupted] = operation =>
                            MapInterruptedState(operation._operation.InterruptReason)
                    });

        private static readonly IReadOnlyDictionary<CoreWebView2DownloadInterruptReason, DownloadTransferState>
            StateByInterruptReason =
                new ReadOnlyDictionary<CoreWebView2DownloadInterruptReason, DownloadTransferState>(
                    new Dictionary<CoreWebView2DownloadInterruptReason, DownloadTransferState>
                    {
                        [CoreWebView2DownloadInterruptReason.UserPaused] = DownloadTransferState.Paused,
                        [CoreWebView2DownloadInterruptReason.UserCanceled] = DownloadTransferState.Canceled
                    });

        private readonly CoreWebView2DownloadOperation _operation;
        private bool _disposed;

        internal WebViewDownloadOperation(CoreWebView2DownloadOperation operation)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _operation.BytesReceivedChanged += OnOperationChanged;
            _operation.EstimatedEndTimeChanged += OnOperationChanged;
            _operation.StateChanged += OnOperationChanged;
        }

        public event EventHandler Changed;

        public string SourceUri => _operation.Uri;

        public string ResultFilePath => _operation.ResultFilePath;

        public long BytesReceived => Math.Max(0L, _operation.BytesReceived);

        public long? TotalBytes
        {
            get
            {
                var value = _operation.TotalBytesToReceive;
                if (!value.HasValue || value.Value == 0UL)
                {
                    return null;
                }

                return value.Value > long.MaxValue
                    ? long.MaxValue
                    : (long)value.Value;
            }
        }

        public DateTime? EstimatedEndTimeUtc
        {
            get
            {
                var value = _operation.EstimatedEndTime;
                return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            }
        }

        public DownloadTransferState State
        {
            get
            {
                return StateByWebViewState.TryGetValue(_operation.State, out var resolveState)
                    ? resolveState(this)
                    : DownloadTransferState.InProgress;
            }
        }

        public bool CanResume => _operation.CanResume;

        public void Pause()
        {
            _operation.Pause();
        }

        public void Resume()
        {
            _operation.Resume();
        }

        public void Cancel()
        {
            _operation.Cancel();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _operation.BytesReceivedChanged -= OnOperationChanged;
            _operation.EstimatedEndTimeChanged -= OnOperationChanged;
            _operation.StateChanged -= OnOperationChanged;
            _disposed = true;
        }

        private static DownloadTransferState MapInterruptedState(
            CoreWebView2DownloadInterruptReason reason)
        {
            return StateByInterruptReason.TryGetValue(reason, out var state)
                ? state
                : DownloadTransferState.Interrupted;
        }

        private void OnOperationChanged(object sender, object args)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
