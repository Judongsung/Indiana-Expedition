using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace IndianaExpedition.Find
{
    internal sealed class WebViewPageFindController : IPageFindController
    {
        private readonly CoreWebView2 _core;
        private readonly CoreWebView2Find _find;
        private PageFindCriteria _criteria;
        private bool _sessionActive;
        private bool _disposed;

        internal WebViewPageFindController(CoreWebView2 core)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _find = core.Find;
            _find.ActiveMatchIndexChanged += OnFindStateChanged;
            _find.MatchCountChanged += OnFindStateChanged;
        }

        public event EventHandler StateChanged;

        public int ActiveMatchIndex => _find.ActiveMatchIndex;

        public int MatchCount => _find.MatchCount;

        public PageFindCriteria CurrentCriteria => _criteria?.Clone();

        public async Task FindAsync(PageFindCriteria criteria)
        {
            ThrowIfDisposed();
            if (criteria == null || string.IsNullOrWhiteSpace(criteria.Term))
            {
                return;
            }

            if (_sessionActive && criteria.Equals(_criteria))
            {
                Move(criteria.SearchUp);
                return;
            }

            await StartSessionAsync(criteria).ConfigureAwait(true);
        }

        public async Task RepeatAsync(bool previous)
        {
            ThrowIfDisposed();
            if (_criteria == null || string.IsNullOrWhiteSpace(_criteria.Term))
            {
                return;
            }

            if (!_sessionActive)
            {
                var repeated = _criteria.Clone();
                repeated.SearchUp = previous;
                await StartSessionAsync(repeated).ConfigureAwait(true);
                return;
            }

            Move(previous);
        }

        public void ResetSession()
        {
            if (_disposed)
            {
                return;
            }

            _find.Stop();
            _sessionActive = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task StartSessionAsync(PageFindCriteria criteria)
        {
            _find.Stop();
            _sessionActive = false;
            _criteria = criteria.Clone();
            var options = _core.Environment.CreateFindOptions();
            options.FindTerm = _criteria.Term;
            options.IsCaseSensitive = _criteria.MatchCase;
            options.ShouldMatchWord = _criteria.MatchWholeWord;
            options.ShouldHighlightAllMatches = false;
            options.SuppressDefaultFindDialog = true;
            await _find.StartAsync(options).ConfigureAwait(true);
            _sessionActive = true;
            if (_criteria.SearchUp && _find.MatchCount > 0)
            {
                _find.FindPrevious();
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Move(bool previous)
        {
            if (previous)
            {
                _find.FindPrevious();
            }
            else
            {
                _find.FindNext();
            }
        }

        private void OnFindStateChanged(object sender, object args)
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebViewPageFindController));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _find.ActiveMatchIndexChanged -= OnFindStateChanged;
            _find.MatchCountChanged -= OnFindStateChanged;
            _find.Stop();
            _disposed = true;
        }
    }
}
