using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal enum XpButtonVariant
    {
        Standard,
        ExplorerHeader,
        AddressBand
    }

    internal sealed class XpButton : Button
    {
        private bool _hovered;
        private bool _mousePressed;
        private bool _keyboardPressed;
        private bool _isDefault;

        internal XpButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint,
                true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            UseVisualStyleBackColor = false;
        }

        internal XpButtonVariant Variant { get; set; }

        public override void NotifyDefault(bool value)
        {
            _isDefault = value;
            Invalidate();
            base.NotifyDefault(value);
        }

        protected override void OnMouseEnter(EventArgs args)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(args);
        }

        protected override void OnMouseLeave(EventArgs args)
        {
            _hovered = false;
            _mousePressed = false;
            Invalidate();
            base.OnMouseLeave(args);
        }

        protected override void OnMouseDown(MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left)
            {
                _mousePressed = true;
                Invalidate();
            }
            base.OnMouseDown(args);
        }

        protected override void OnMouseUp(MouseEventArgs args)
        {
            _mousePressed = false;
            Invalidate();
            base.OnMouseUp(args);
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Space)
            {
                _keyboardPressed = true;
                Invalidate();
            }
            base.OnKeyDown(args);
        }

        protected override void OnKeyUp(KeyEventArgs args)
        {
            if (args.KeyCode == Keys.Space)
            {
                _keyboardPressed = false;
                Invalidate();
            }
            base.OnKeyUp(args);
        }

        protected override void OnEnabledChanged(EventArgs args)
        {
            Invalidate();
            base.OnEnabledChanged(args);
        }

        protected override void OnGotFocus(EventArgs args)
        {
            Invalidate();
            base.OnGotFocus(args);
        }

        protected override void OnLostFocus(EventArgs args)
        {
            _keyboardPressed = false;
            Invalidate();
            base.OnLostFocus(args);
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaintBackground(args);
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Variant == XpButtonVariant.ExplorerHeader)
            {
                PaintExplorerHeaderButton(args.Graphics);
                return;
            }

            if (Variant == XpButtonVariant.AddressBand)
            {
                PaintAddressBandButton(args.Graphics);
                return;
            }

            PaintStandardButton(args.Graphics);
        }

        private void PaintStandardButton(Graphics graphics)
        {
            var pressed = _mousePressed || _keyboardPressed;
            var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            var top = !Enabled
                ? XpPalette.ButtonDisabledTop
                : pressed
                    ? XpPalette.ButtonPressedTop
                    : _hovered
                        ? XpPalette.ButtonHotTop
                        : XpPalette.ButtonNormalTop;
            var bottom = !Enabled
                ? XpPalette.ButtonDisabledBottom
                : pressed
                    ? XpPalette.ButtonPressedBottom
                    : _hovered
                        ? XpPalette.ButtonHotBottom
                        : XpPalette.ButtonNormalBottom;
            var borderColor = !Enabled
                ? XpPalette.ButtonDisabledBorder
                : _hovered
                    ? XpPalette.ButtonHotBorder
                    : XpPalette.ButtonBorder;

            using (var path = XpDrawing.CreateRoundedRectangle(bounds, LunaMetrics.StandardButtonCornerRadius))
            using (var fill = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
            using (var border = new Pen(borderColor))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(border, path);
            }

            var innerBounds = Rectangle.Inflate(bounds, -1, -1);
            using (var innerPath = XpDrawing.CreateRoundedRectangle(innerBounds, LunaMetrics.StandardButtonCornerRadius - 1))
            using (var highlight = new Pen(XpPalette.ButtonInnerHighlight))
            {
                graphics.DrawPath(highlight, innerPath);
            }

            if (_isDefault || Focused)
            {
                var focusBorder = Rectangle.Inflate(bounds, -2, -2);
                using (var focusPath = XpDrawing.CreateRoundedRectangle(focusBorder, 1))
                using (var focusPen = new Pen(XpPalette.ButtonBorder))
                {
                    graphics.DrawPath(focusPen, focusPath);
                }
            }

            var offset = pressed ? 1 : 0;
            var content = Rectangle.Inflate(ClientRectangle, -5, -3);
            content.Offset(offset, offset);
            DrawContent(graphics, content);

            if (Focused && ShowFocusCues && !string.IsNullOrEmpty(Text))
            {
                var focus = Rectangle.Inflate(content, -2, -2);
                ControlPaint.DrawFocusRectangle(graphics, focus, ForeColor, Color.Transparent);
            }
        }

        private void PaintExplorerHeaderButton(Graphics graphics)
        {
            var bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            if (_hovered || _mousePressed)
            {
                var top = _mousePressed ? XpPalette.LunaCloseLower : XpPalette.LunaCloseUpper;
                var bottom = _mousePressed ? XpPalette.LunaCloseTop : XpPalette.LunaCloseBottom;
                using (var path = XpDrawing.CreateRoundedRectangle(bounds, LunaMetrics.StandardButtonCornerRadius))
                using (var fill = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
                using (var border = new Pen(XpPalette.ExplorerHeaderText))
                {
                    graphics.FillPath(fill, path);
                    graphics.DrawPath(border, path);
                }
            }

            var textBounds = ClientRectangle;
            if (_mousePressed)
            {
                textBounds.Offset(1, 1);
            }
            using (var boldFont = new Font(Font, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    graphics,
                    Text,
                    boldFont,
                    textBounds,
                    XpPalette.ExplorerHeaderText,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.SingleLine);
            }
        }

        private void PaintAddressBandButton(Graphics graphics)
        {
            var pressed = _mousePressed || _keyboardPressed;
            using (var separator = new Pen(XpPalette.ToolbarBorder))
            using (var separatorHighlight = new Pen(XpPalette.ToolbarInnerBorder))
            {
                graphics.DrawLine(separator, 0, 2, 0, Height - 3);
                graphics.DrawLine(separatorHighlight, 1, 2, 1, Height - 3);
            }

            if (_hovered || pressed)
            {
                var selectionBounds = new Rectangle(
                    3,
                    1,
                    Math.Max(1, Width - 4),
                    Math.Max(1, Height - 2));
                var top = pressed ? XpPalette.SelectionDark : XpPalette.SelectionLight;
                var bottom = pressed ? XpPalette.SelectionLight : XpPalette.SelectionDark;
                using (var path = XpDrawing.CreateRoundedRectangle(
                           selectionBounds,
                           LunaMetrics.ToolbarItemCornerRadius))
                using (var fill = new LinearGradientBrush(
                           selectionBounds,
                           top,
                           bottom,
                           LinearGradientMode.Vertical))
                using (var border = new Pen(XpPalette.MenuItemBorder))
                {
                    graphics.FillPath(fill, path);
                    graphics.DrawPath(border, path);
                }
            }

            var content = new Rectangle(
                6,
                1,
                Math.Max(0, Width - 8),
                Math.Max(0, Height - 2));
            if (pressed)
            {
                content.Offset(1, 1);
            }
            DrawContent(graphics, content);

            if (Focused && ShowFocusCues && !string.IsNullOrEmpty(Text))
            {
                var focus = Rectangle.Inflate(content, -1, -3);
                ControlPaint.DrawFocusRectangle(graphics, focus, ForeColor, Color.Transparent);
            }
        }

        private void DrawContent(Graphics graphics, Rectangle content)
        {
            if (Image != null)
            {
                var imageBounds = XpDrawing.AlignRectangle(Image.Size, content, ImageAlign);
                if (Enabled)
                {
                    graphics.DrawImage(Image, imageBounds);
                }
                else
                {
                    ControlPaint.DrawImageDisabled(graphics, Image, imageBounds.X, imageBounds.Y, XpPalette.ControlFace);
                }
            }

            if (!string.IsNullOrEmpty(Text))
            {
                var textColor = Enabled ? ForeColor : SystemColors.GrayText;
                TextRenderer.DrawText(
                    graphics,
                    Text,
                    Font,
                    content,
                    textColor,
                    XpDrawing.GetTextFormatFlags(TextAlign, ShowKeyboardCues));
            }
        }
    }
}
