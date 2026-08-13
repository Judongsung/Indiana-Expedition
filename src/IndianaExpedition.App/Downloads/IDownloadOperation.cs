using System;

namespace IndianaExpedition.Downloads
{
    internal enum DownloadTransferState
    {
        InProgress,
        Paused,
        Interrupted,
        Completed,
        Canceled
    }

    internal interface IDownloadOperation : IDisposable
    {
        event EventHandler Changed;

        string SourceUri { get; }

        string ResultFilePath { get; }

        long BytesReceived { get; }

        long? TotalBytes { get; }

        DateTime? EstimatedEndTimeUtc { get; }

        DownloadTransferState State { get; }

        bool CanResume { get; }

        void Pause();

        void Resume();

        void Cancel();
    }

    internal interface IDownloadController
    {
        event EventHandler Changed;

        string FileName { get; }

        string FilePath { get; }

        string SourceHost { get; }

        long BytesReceived { get; }

        long? TotalBytes { get; }

        DateTime? EstimatedEndTimeUtc { get; }

        DownloadTransferState State { get; }

        bool CanPause { get; }

        bool CanResume { get; }

        bool IsFinished { get; }

        void Pause();

        void Resume();

        void Cancel();
    }
}
