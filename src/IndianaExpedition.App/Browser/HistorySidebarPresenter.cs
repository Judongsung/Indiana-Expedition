using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Core.Models;
using IndianaExpedition.Core.Services;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Browser
{
    internal sealed class HistorySidebarPresenter
    {
        private readonly TreeView _tree;
        private readonly Func<IReadOnlyList<HistoryEntry>> _getItems;

        internal HistorySidebarPresenter(
            TreeView tree,
            Func<IReadOnlyList<HistoryEntry>> getItems)
        {
            _tree = tree ?? throw new ArgumentNullException(nameof(tree));
            _getItems = getItems ?? throw new ArgumentNullException(nameof(getItems));
        }

        internal void Rebuild()
        {
            var today = DateTime.Now.Date;
            var groups = _getItems()
                .Select(entry => new { Entry = entry, LocalTime = entry.VisitedAtUtc.ToLocalTime() })
                .GroupBy(item => item.LocalTime.Date)
                .OrderByDescending(group => group.Key);
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                foreach (var group in groups)
                {
                    var dayNode = CreateDayNode(group.Key, today);
                    foreach (var item in group.OrderByDescending(value => value.LocalTime))
                    {
                        dayNode.Nodes.Add(CreateEntryNode(item.Entry));
                    }
                    _tree.Nodes.Add(dayNode);
                    dayNode.Expand();
                }
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        internal void Apply(HistoryChangedEventArgs change)
        {
            if (change == null || change.Kind == HistoryChangeKind.Reset)
            {
                Rebuild();
                return;
            }

            _tree.BeginUpdate();
            try
            {
                foreach (var removed in change.RemovedEntries)
                {
                    RemoveEntry(removed);
                }
                InsertEntry(change.Entry);
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        private void InsertEntry(HistoryEntry entry)
        {
            var localTime = entry.VisitedAtUtc.ToLocalTime();
            var dayNode = _tree.Nodes.Cast<TreeNode>()
                .FirstOrDefault(node => node.Tag is DateTime day && day == localTime.Date);
            if (dayNode == null)
            {
                dayNode = CreateDayNode(localTime.Date, DateTime.Now.Date);
                var dayIndex = 0;
                while (dayIndex < _tree.Nodes.Count &&
                       _tree.Nodes[dayIndex].Tag is DateTime existingDay &&
                       existingDay > localTime.Date)
                {
                    dayIndex++;
                }
                _tree.Nodes.Insert(dayIndex, dayNode);
                dayNode.Expand();
            }

            var entryNode = CreateEntryNode(entry);
            var index = 0;
            while (index < dayNode.Nodes.Count &&
                   dayNode.Nodes[index].Tag is HistoryEntry existing &&
                   existing.VisitedAtUtc >= entry.VisitedAtUtc)
            {
                index++;
            }
            dayNode.Nodes.Insert(index, entryNode);
        }

        private void RemoveEntry(HistoryEntry entry)
        {
            foreach (TreeNode dayNode in _tree.Nodes)
            {
                var match = dayNode.Nodes.Cast<TreeNode>().FirstOrDefault(node =>
                    node.Tag is HistoryEntry existing &&
                    string.Equals(existing.Url, entry.Url, StringComparison.OrdinalIgnoreCase) &&
                    existing.VisitedAtUtc == entry.VisitedAtUtc);
                if (match != null)
                {
                    dayNode.Nodes.Remove(match);
                    RemoveEmptyDay(dayNode);
                    return;
                }
            }
        }

        private void RemoveEmptyDay(TreeNode dayNode)
        {
            if (dayNode.Nodes.Count == 0)
            {
                _tree.Nodes.Remove(dayNode);
            }
        }

        private static TreeNode CreateDayNode(DateTime day, DateTime today)
        {
            return new TreeNode(
                FormatDate(day, today),
                BrowserUiConstants.HistoryImageIndex,
                BrowserUiConstants.HistoryImageIndex) { Tag = day };
        }

        private static TreeNode CreateEntryNode(HistoryEntry entry)
        {
            var localTime = entry.VisitedAtUtc.ToLocalTime();
            var text = string.Format(
                CultureInfo.CurrentCulture,
                Strings.HistoryEntryFormat,
                entry.Title,
                localTime.ToString(BrowserUiConstants.HistoryTimeFormat, CultureInfo.CurrentCulture));
            return new TreeNode(
                text,
                BrowserUiConstants.PageImageIndex,
                BrowserUiConstants.PageImageIndex) { Tag = entry.Clone() };
        }

        private static string FormatDate(DateTime date, DateTime today)
        {
            if (date == today)
            {
                return Strings.HistoryToday;
            }
            if (date == today.AddDays(-1))
            {
                return Strings.HistoryYesterday;
            }
            return date.ToString(BrowserUiConstants.HistoryDateFormat, CultureInfo.CurrentCulture);
        }
    }
}
