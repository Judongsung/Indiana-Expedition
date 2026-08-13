using System;

namespace IndianaExpedition
{
    internal sealed class PageFindCriteria : IEquatable<PageFindCriteria>
    {
        internal string Term { get; set; } = string.Empty;

        internal bool SearchUp { get; set; }

        internal bool MatchCase { get; set; }

        internal bool MatchWholeWord { get; set; }

        public bool Equals(PageFindCriteria other)
        {
            return other != null &&
                   string.Equals(Term, other.Term, StringComparison.Ordinal) &&
                   SearchUp == other.SearchUp &&
                   MatchCase == other.MatchCase &&
                   MatchWholeWord == other.MatchWholeWord;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PageFindCriteria);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(Term ?? string.Empty);
                hash = (hash * 397) ^ SearchUp.GetHashCode();
                hash = (hash * 397) ^ MatchCase.GetHashCode();
                hash = (hash * 397) ^ MatchWholeWord.GetHashCode();
                return hash;
            }
        }

        internal PageFindCriteria Clone()
        {
            return new PageFindCriteria
            {
                Term = Term ?? string.Empty,
                SearchUp = SearchUp,
                MatchCase = MatchCase,
                MatchWholeWord = MatchWholeWord
            };
        }
    }
}
