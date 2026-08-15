using System;
using System.Collections.Generic;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Browser
{
    internal sealed class RecentAddressHistory
    {
        private readonly List<string> _items = new List<string>();

        internal IReadOnlyList<string> Items => _items;

        internal bool Remember(string address)
        {
            var value = (address ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return false;
            }

            var existingIndex = _items.FindIndex(item =>
                string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
            if (existingIndex == 0)
            {
                return false;
            }
            if (existingIndex > 0)
            {
                _items.RemoveAt(existingIndex);
            }
            _items.Insert(0, value);
            if (_items.Count > RecentAddressConstants.MaximumEntries)
            {
                _items.RemoveRange(
                    RecentAddressConstants.MaximumEntries,
                    _items.Count - RecentAddressConstants.MaximumEntries);
            }
            return true;
        }
    }
}
