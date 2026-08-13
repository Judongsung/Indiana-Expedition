using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndianaExpedition.Resources;
using IndianaExpedition.Styling;

namespace IndianaExpedition
{
    internal sealed class PageFindDialog : LunaForm
    {
        private readonly IPageFindController _controller;
        private readonly TextBox _termBox;
        private readonly RadioButton _upButton;
        private readonly RadioButton _downButton;
        private readonly CheckBox _matchCaseBox;
        private readonly CheckBox _wholeWordBox;
        private readonly Label _resultLabel;
        private readonly XpButton _findButton;

        internal PageFindDialog(
            IPageFindController controller,
            PageFindCriteria initialCriteria,
            bool preventActivationOnShow = false)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            var criteria = initialCriteria?.Clone() ?? new PageFindCriteria();
            PreventActivationOnShow = preventActivationOnShow;
            Text = Strings.FindDialogTitle;
            SetContentClientSize(472, 238);
            LunaResizable = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = preventActivationOnShow;
            StartPosition = FormStartPosition.CenterParent;

            var termLabel = new Label { Text = Strings.FindWhat, AutoSize = true, Location = new Point(16, 20) };
            _termBox = new TextBox { Text = criteria.Term, Location = new Point(16, 44), Size = new Size(326, 23) };
            _termBox.TextChanged += (sender, args) => UpdateFindButton();
            _findButton = new XpButton { Text = Strings.FindNext, Location = new Point(354, 42), Size = new Size(102, 27) };
            _findButton.Click += async (sender, args) => await FindNextAsync().ConfigureAwait(true);

            var directionGroup = new GroupBox { Text = Strings.FindDirection, Location = new Point(16, 82), Size = new Size(206, 82) };
            _upButton = new RadioButton { Text = Strings.FindUp, AutoSize = true, Location = new Point(20, 34), Checked = criteria.SearchUp };
            _downButton = new RadioButton { Text = Strings.FindDown, AutoSize = true, Location = new Point(108, 34), Checked = !criteria.SearchUp };
            directionGroup.Controls.AddRange(new Control[] { _upButton, _downButton });

            var optionsGroup = new GroupBox { Text = Strings.FindOptions, Location = new Point(232, 82), Size = new Size(224, 82) };
            _matchCaseBox = new CheckBox { Text = Strings.FindMatchCase, AutoSize = true, Location = new Point(16, 24), Checked = criteria.MatchCase };
            _wholeWordBox = new CheckBox { Text = Strings.FindWholeWord, AutoSize = true, Location = new Point(16, 50), Checked = criteria.MatchWholeWord };
            optionsGroup.Controls.AddRange(new Control[] { _matchCaseBox, _wholeWordBox });

            _resultLabel = new Label
            {
                Text = Strings.FindReady,
                Location = new Point(16, 178),
                Size = new Size(326, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            var close = new XpButton
            {
                Text = Strings.Close,
                Location = new Point(354, 176),
                Size = new Size(102, 27),
                DialogResult = DialogResult.Cancel
            };

            ContentPanel.Controls.AddRange(new Control[]
            {
                termLabel, _termBox, _findButton, directionGroup, optionsGroup, _resultLabel, close
            });
            AcceptButton = _findButton;
            CancelButton = close;
            _controller.StateChanged += OnFindStateChanged;
            UpdateFindButton();
            UpdateResult();
        }

        internal PageFindCriteria Criteria => new PageFindCriteria
        {
            Term = _termBox.Text,
            SearchUp = _upButton.Checked,
            MatchCase = _matchCaseBox.Checked,
            MatchWholeWord = _wholeWordBox.Checked
        };

        private async Task FindNextAsync()
        {
            if (string.IsNullOrWhiteSpace(_termBox.Text))
            {
                _termBox.Focus();
                return;
            }

            SetBusy(true);
            try
            {
                await _controller.FindAsync(Criteria).ConfigureAwait(true);
                UpdateResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Branding.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            _findButton.Enabled = !busy && !string.IsNullOrWhiteSpace(_termBox.Text);
        }

        private void UpdateFindButton()
        {
            _findButton.Enabled = !string.IsNullOrWhiteSpace(_termBox.Text);
        }

        private void OnFindStateChanged(object sender, EventArgs args)
        {
            if (IsDisposed)
            {
                return;
            }
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateResult));
                return;
            }
            UpdateResult();
        }

        private void UpdateResult()
        {
            if (string.IsNullOrWhiteSpace(_termBox.Text))
            {
                _resultLabel.Text = Strings.FindReady;
                return;
            }

            var count = _controller.MatchCount;
            var active = _controller.ActiveMatchIndex;
            if (count <= 0)
            {
                _resultLabel.Text = Strings.FindNoResults;
                return;
            }

            _resultLabel.Text = string.Format(
                CultureInfo.CurrentCulture,
                Strings.FindResultFormat,
                Math.Max(1, active),
                count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _controller.StateChanged -= OnFindStateChanged;
            }
            base.Dispose(disposing);
        }
    }
}
