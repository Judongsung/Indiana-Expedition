using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IndianaExpedition.Core.Models;

namespace IndianaExpedition.Downloads
{
    internal sealed class DownloadSession : IDownloadController, IDisposable
    {
        private static readonly IReadOnlyDictionary<DownloadRecordState, DownloadTransferState>
            TransferStateByRecordState =
                new ReadOnlyDictionary<DownloadRecordState, DownloadTransferState>(
                    new Dictionary<DownloadRecordState, DownloadTransferState>
                    {
                        [DownloadRecordState.Completed] = DownloadTransferState.Completed,
                        [DownloadRecordState.Canceled] = DownloadTransferState.Canceled,
                        [DownloadRecordState.Failed] = DownloadTransferState.Interrupted
                    });
        private readonly IDownloadOperation _operation;
        private readonly DateTime _startedAtUtc;
        private DownloadRecordState? _finalState;
        private bool _disposed;

        internal DownloadSession(IDownloadOperation operation, DateTime startedAtUtc)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _startedAtUtc = startedAtUtc.Kind == DateTimeKind.Utc
                ? startedAtUtc
                : startedAtUtc.ToUniversalTime();
            _operation.Changed += OnOperationChanged;
            CompleteFromOperationIfNeeded();
        }

        public event EventHandler Changed;

        internal event EventHandler Finished;

        public string FileName => Path.GetFileName(FilePath);

        public string FilePath => _operation.ResultFilePath;

        public string SourceHost
        {
            get
            {
                return Uri.TryCreate(_operation.SourceUri, UriKind.Absolute, out var uri)
                    ? uri.Host
                    : string.Empty;
            }
        }

        public long BytesReceived => _operation.BytesReceived;

        public long? TotalBytes => _operation.TotalBytes;

        public DateTime? EstimatedEndTimeUtc => _operation.EstimatedEndTimeUtc;

        public DownloadTransferState State => _finalState.HasValue
            ? MapFinalState(_finalState.Value)
            : _operation.State;

        public bool CanPause => !_finalState.HasValue &&
                                _operation.State == DownloadTransferState.InProgress;

        public bool CanResume => !_finalState.HasValue &&
                                 (_operation.State == DownloadTransferState.Paused ||
                                  _operation.State == DownloadTransferState.Interrupted) &&
                                 _operation.CanResume;

        public bool IsFinished => _finalState.HasValue;

        public void Pause()
        {
            if (CanPause)
            {
                _operation.Pause();
            }
        }

        public void Resume()
        {
            if (CanResume)
            {
                _operation.Resume();
            }
        }

        public void Cancel()
        {
            if (_finalState.HasValue)
            {
                return;
            }

            _operation.Cancel();
            Finish(DownloadRecordState.Canceled);
        }

        internal DownloadRecord CreateRecord()
        {
            if (!_finalState.HasValue)
            {
                throw new InvalidOperationException(
                    Constants.ApplicationConstants.DownloadSessionNotFinishedMessage);
            }

            return new DownloadRecord
            {
                FileName = FileName,
                FilePath = FilePath,
                StartedAtUtc = _startedAtUtc,
                FinishedAtUtc = DateTime.UtcNow,
                BytesReceived = BytesReceived,
                TotalBytes = TotalBytes,
                State = _finalState.Value
            };
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _operation.Changed -= OnOperationChanged;
            _operation.Dispose();
            _disposed = true;
        }

        private void OnOperationChanged(object sender, EventArgs args)
        {
            if (!CompleteFromOperationIfNeeded())
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CompleteFromOperationIfNeeded()
        {
            if (_finalState.HasValue)
            {
                return false;
            }

            switch (_operation.State)
            {
                case DownloadTransferState.Completed:
                    return Finish(DownloadRecordState.Completed);
                case DownloadTransferState.Canceled:
                    return Finish(DownloadRecordState.Canceled);
                case DownloadTransferState.Interrupted when !_operation.CanResume:
                    return Finish(DownloadRecordState.Failed);
                default:
                    return false;
            }
        }

        private bool Finish(DownloadRecordState state)
        {
            if (_finalState.HasValue)
            {
                return false;
            }

            _finalState = state;
            Changed?.Invoke(this, EventArgs.Empty);
            Finished?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private static DownloadTransferState MapFinalState(DownloadRecordState state)
        {
            return TransferStateByRecordState.TryGetValue(state, out var transferState)
                ? transferState
                : DownloadTransferState.Interrupted;
        }
    }
}
