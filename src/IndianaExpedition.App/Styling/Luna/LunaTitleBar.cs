using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IndianaExpedition.Constants;
using IndianaExpedition.Resources;

namespace IndianaExpedition.Styling
{
    internal sealed class LunaTitleBar : Control
    {
        private readonly LunaForm _owner;
        private readonly LunaCaptionButton _minimizeButton;
        private readonly LunaCaptionButton _maximizeButton;
        private readonly LunaCaptionButton _closeButton;
        private readonly ToolTip _toolTip;
        private LunaCaptionLayout _captionLayout;
        private bool _active = true;
        private DateTime _lastCaptionClickUtc = DateTime.MinValue;
        private Point _lastCaptionClickLocation;

        internal LunaTitleBar(LunaForm owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);

            Height = LunaMetrics.CaptionHeight;
            Dock = DockStyle.Top;
            TabStop = false;

            _minimizeButton = CreateButton(LunaCaptionButtonKind.Minimize, (sender, args) => _owner.MinimizeWindow());
            _maximizeButton = CreateButton(LunaCaptionButtonKind.Maximize, (sender, args) => _owner.ToggleMaximizeWindow());
            _closeButton = CreateButton(LunaCaptionButtonKind.Close, (sender, args) => _owner.Close());
            Controls.AddRange(new Control[] { _minimizeButton, _maximizeButton, _closeButton });

            _toolTip = new ToolTip
            {
                AutomaticDelay = 500,
                AutoPopDelay = 5000,
                ReshowDelay = 100,
                ShowAlways = true
            };
            _toolTip.SetToolTip(_minimizeButton, Strings.MinimizeWindow);
            _toolTip.SetToolTip(_maximizeButton, Strings.MaximizeWindow);
            _toolTip.SetToolTip(_closeButton, Strings.CloseWindow);
        }

        internal void SetActive(bool active)
        {
            if (_active == active)
            {
                return;
            }

            _active = active;
            _minimizeButton.IsWindowActive = active;
            _maximizeButton.IsWindowActive = active;
            _closeButton.IsWindowActive = active;
            _minimizeButton.Invalidate();
            _maximizeButton.Invalidate();
            _closeButton.Invalidate();
            Invalidate();
        }

        internal void RefreshWindowState()
        {
            if (_maximizeButton == null || _toolTip == null)
            {
                return;
            }

            _maximizeButton.DrawRestoreGlyph = _owner.WindowState == FormWindowState.Maximized;
            _toolTip.SetToolTip(
                _maximizeButton,
                _maximizeButton.DrawRestoreGlyph ? Strings.RestoreWindow : Strings.MaximizeWindow);
            PerformLayout();
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs args)
        {
            base.OnLayout(args);

            if (_minimizeButton == null || _maximizeButton == null || _closeButton == null)
            {
                return;
            }

            var showMinimize = _owner.MinimizeBox;
            var showMaximize = _owner.MaximizeBox;
            _minimizeButton.Visible = showMinimize;
            _maximizeButton.Visible = showMaximize;
            _captionLayout = LunaLayout.CalculateCaption(
                ClientSize,
                _owner.ShowIcon && _owner.Icon != null,
                showMinimize,
                showMaximize);
            _closeButton.Bounds = _captionLayout.CloseButtonBounds;
            _maximizeButton.Bounds = _captionLayout.MaximizeButtonBounds;
            _minimizeButton.Bounds = _captionLayout.MinimizeButtonBounds;
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            var bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var bodyBounds = new Rectangle(
                bounds.X,
                LunaMetrics.CaptionTopRimHeight,
                bounds.Width,
                Math.Max(
                    1,
                    bounds.Height - LunaMetrics.CaptionTopRimHeight - LunaMetrics.CaptionBottomRimHeight));
            var top = _active ? XpPalette.LunaCaptionTop : XpPalette.LunaInactiveTop;
            var bottom = _active ? XpPalette.LunaCaptionBottom : XpPalette.LunaInactiveBottom;
            using (var brush = new LinearGradientBrush(bodyBounds, top, bottom, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend
                {
                    Colors = _active
                        ? new[]
                        {
                            XpPalette.LunaCaptionTop,
                            XpPalette.LunaCaptionUpper,
                            XpPalette.LunaCaptionShoulder,
                            XpPalette.LunaCaptionBodyTop,
                            XpPalette.LunaCaptionMiddle,
                            XpPalette.LunaCaptionMiddle,
                            XpPalette.LunaCaptionLower,
                            XpPalette.LunaCaptionBottom,
                            XpPalette.LunaCaptionBottom
                        }
                        : new[] { top, XpPalette.LunaInactiveMiddle, bottom },
                    Positions = _active
                        ? new[]
                        {
                            0f,
                            LunaMetrics.CaptionGradientUpperPosition,
                            LunaMetrics.CaptionGradientShoulderPosition,
                            LunaMetrics.CaptionGradientBodyTopPosition,
                            LunaMetrics.CaptionGradientMiddlePosition,
                            LunaMetrics.CaptionGradientPlateauPosition,
                            LunaMetrics.CaptionGradientLowerPosition,
                            LunaMetrics.CaptionGradientBottomPosition,
                            1f
                        }
                        : new[] { 0f, LunaMetrics.InactiveCaptionGradientMiddlePosition, 1f }
                };
                brush.InterpolationColors = blend;
                args.Graphics.FillRectangle(brush, bodyBounds);
            }

            var outerTop = _active ? XpPalette.LunaCaptionOuterTop : XpPalette.LunaInactiveBottom;
            var highlight = _active ? XpPalette.LunaCaptionHighlight : XpPalette.LunaInactiveTop;
            var lowerEdge = _active ? XpPalette.LunaCaptionLowerEdge : XpPalette.LunaInactiveBottom;
            var bottomEdge = _active ? XpPalette.LunaCaptionBottomEdge : XpPalette.LunaInactiveBottom;
            using (var outerTopPen = new Pen(outerTop))
            using (var highlightPen = new Pen(highlight))
            using (var lowerPen = new Pen(lowerEdge))
            using (var bottomPen = new Pen(bottomEdge))
            {
                var highlightY = LunaMetrics.CaptionTopRimHeight - 1;
                var lowerEdgeY = Height - LunaMetrics.CaptionBottomRimHeight;
                args.Graphics.DrawLine(outerTopPen, 0, 0, Width - 1, 0);
                args.Graphics.DrawLine(highlightPen, 0, highlightY, Width - 1, highlightY);
                args.Graphics.DrawLine(lowerPen, 0, lowerEdgeY, Width - 1, lowerEdgeY);
                args.Graphics.DrawLine(bottomPen, 0, Height - 1, Width - 1, Height - 1);
            }

            if (_closeButton != null)
            {
                DrawCaption(args.Graphics);
            }
        }

        protected override void OnMouseDown(MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left)
            {
                var now = DateTime.UtcNow;
                var doubleClickArea = new Rectangle(
                    _lastCaptionClickLocation.X - (SystemInformation.DoubleClickSize.Width / 2),
                    _lastCaptionClickLocation.Y - (SystemInformation.DoubleClickSize.Height / 2),
                    SystemInformation.DoubleClickSize.Width,
                    SystemInformation.DoubleClickSize.Height);
                var isDoubleClick = _owner.MaximizeBox &&
                                    (now - _lastCaptionClickUtc).TotalMilliseconds <=
                                    SystemInformation.DoubleClickTime &&
                                    doubleClickArea.Contains(args.Location);

                _lastCaptionClickUtc = now;
                _lastCaptionClickLocation = args.Location;
                if (isDoubleClick)
                {
                    _lastCaptionClickUtc = DateTime.MinValue;
                    _owner.ToggleMaximizeWindow();
                    base.OnMouseDown(args);
                    return;
                }

                _owner.BeginWindowDrag();
            }
            base.OnMouseDown(args);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip?.Dispose();
            }
            base.Dispose(disposing);
        }

        private LunaCaptionButton CreateButton(LunaCaptionButtonKind kind, EventHandler click)
        {
            var button = new LunaCaptionButton(kind)
            {
                Size = new Size(
                    kind == LunaCaptionButtonKind.Close
                        ? LunaMetrics.CaptionCloseButtonWidth
                        : LunaMetrics.CaptionButtonWidth,
                    LunaMetrics.CaptionButtonHeight)
            };
            button.Click += click;
            return button;
        }

        private void DrawCaption(Graphics graphics)
        {
            if (_captionLayout == null)
            {
                _captionLayout = LunaLayout.CalculateCaption(
                    ClientSize,
                    _owner.ShowIcon && _owner.Icon != null,
                    _owner.MinimizeBox,
                    _owner.MaximizeBox);
            }

            if (!_captionLayout.IconBounds.IsEmpty && _owner.Icon != null)
            {
                graphics.DrawIcon(_owner.Icon, _captionLayout.IconBounds);
            }

            var textBounds = _captionLayout.TextBounds;
            var flags = TextFormatFlags.Left |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.NoPrefix |
                        TextFormatFlags.SingleLine;
            TextRenderer.DrawText(
                graphics,
                _owner.Text,
                _owner.CaptionFont,
                new Rectangle(
                    textBounds.X + LunaMetrics.CaptionTextShadowOffset,
                    textBounds.Y + LunaMetrics.CaptionTextShadowOffset,
                    textBounds.Width,
                    textBounds.Height),
                XpPalette.LunaCaptionTextShadow,
                flags);
            TextRenderer.DrawText(graphics, _owner.Text, _owner.CaptionFont, textBounds, Color.White, flags);
        }
    }
}
