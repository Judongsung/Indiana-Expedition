using System;
using System.Collections.Generic;
using System.Linq;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Constants;
using IndianaExpedition.Core.Services;

namespace IndianaExpedition.Popups
{
    internal sealed class PopupBlockerStateChangedEventArgs : EventArgs
    {
        internal PopupBlockerStateChangedEventArgs(int count, bool canOpen, bool canAllowOrigin)
        {
            Count = count;
            CanOpen = canOpen;
            CanAllowOrigin = canAllowOrigin;
        }

        internal int Count { get; }
        internal bool CanOpen { get; }
        internal bool CanAllowOrigin { get; }
    }

    internal sealed class PopupBlockerPresenter
    {
        private readonly Queue<BlockedPopupRequest> _pending = new Queue<BlockedPopupRequest>();
        private readonly SettingsService _settings;
        private readonly Action<string> _openWindow;

        internal PopupBlockerPresenter(SettingsService settings, Action<string> openWindow)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _openWindow = openWindow ?? throw new ArgumentNullException(nameof(openWindow));
        }

        internal event EventHandler<PopupBlockerStateChangedEventArgs> StateChanged;

        internal int PendingCount => _pending.Count;

        internal void Enqueue(string sourceOrigin, string targetUri)
        {
            while (_pending.Count >= PopupUiConstants.MaximumPendingPopups)
            {
                _pending.Dequeue();
            }
            _pending.Enqueue(new BlockedPopupRequest(sourceOrigin, targetUri));
            PublishState();
        }

        internal void OpenOldest()
        {
            while (_pending.Count > 0)
            {
                var request = _pending.Dequeue();
                if (IsOpenableTarget(request.TargetUri))
                {
                    _openWindow(request.TargetUri);
                    break;
                }
            }
            PublishState();
        }

        internal bool TryAllowOldestOrigin(out int maximumOrigins)
        {
            maximumOrigins = PopupPolicyConstants.MaximumAllowedOrigins;
            var origin = _pending
                .Select(request => request.SourceOrigin)
                .FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
            if (string.IsNullOrWhiteSpace(origin))
            {
                return true;
            }

            var origins = _settings.Current.AllowedPopupOrigins;
            if (!origins.Contains(origin, StringComparer.OrdinalIgnoreCase) &&
                origins.Count >= maximumOrigins)
            {
                return false;
            }

            _settings.Update(settings => settings.AllowedPopupOrigins.Add(origin));
            var remaining = _pending
                .Where(request => !string.Equals(request.SourceOrigin, origin, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            _pending.Clear();
            foreach (var request in remaining)
            {
                _pending.Enqueue(request);
            }
            PublishState();
            return true;
        }

        internal void Dismiss()
        {
            _pending.Clear();
            PublishState();
        }

        private void PublishState()
        {
            StateChanged?.Invoke(
                this,
                new PopupBlockerStateChangedEventArgs(
                    _pending.Count,
                    _pending.Any(request => IsOpenableTarget(request.TargetUri)),
                    _pending.Any(request => !string.IsNullOrWhiteSpace(request.SourceOrigin))));
        }

        private static bool IsOpenableTarget(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private sealed class BlockedPopupRequest
        {
            internal BlockedPopupRequest(string sourceOrigin, string targetUri)
            {
                SourceOrigin = sourceOrigin;
                TargetUri = targetUri;
            }

            internal string SourceOrigin { get; }
            internal string TargetUri { get; }
        }
    }
}
