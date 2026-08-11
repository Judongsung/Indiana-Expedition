using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal enum LunaCaptionButtonKind
    {
        Minimize,
        Maximize,
        Close
    }

    internal sealed class LunaCaptionButton : Control
    {
        private readonly LunaCaptionButtonKind _kind;
        private bool _hovered;
        private bool _pressed;

        internal LunaCaptionButton(LunaCaptionButtonKind kind)
        {
            _kind = kind;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.UserPaint,
                true);
            TabStop = false;
            Cursor = Cursors.Default;
        }

        internal bool DrawRestoreGlyph { get; set; }

        internal bool IsWindowActive { get; set; } = true;

        protected override void OnMouseEnter(EventArgs args)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(args);
        }

        protected override void OnMouseLeave(EventArgs args)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(args);
        }

        protected override void OnMouseDown(MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
            base.OnMouseDown(args);
        }

        protected override void OnMouseUp(MouseEventArgs args)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(args);
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            var isClose = _kind == LunaCaptionButtonKind.Close;
            GetGradientColors(
                isClose,
                out var top,
                out var shoulder,
                out var peak,
                out var upper,
                out var middle,
                out var lower,
                out var bottom);

            if (_pressed)
            {
                Swap(ref top, ref bottom);
                Swap(ref shoulder, ref lower);
                Swap(ref peak, ref middle);
            }

            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = XpDrawing.CreateRoundedRectangle(bounds, LunaMetrics.CaptionButtonCornerRadius))
            using (var brush = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
            using (var border = new Pen(
                       isClose ? XpPalette.LunaCloseButtonBorder : XpPalette.LunaButtonBorder))
            {
                brush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { top, shoulder, peak, upper, middle, lower, bottom },
                    Positions = new[]
                    {
                        0f,
                        LunaMetrics.CaptionButtonGradientShoulderPosition,
                        LunaMetrics.CaptionButtonGradientPeakPosition,
                        LunaMetrics.CaptionButtonGradientUpperPosition,
                        LunaMetrics.CaptionButtonGradientMiddlePosition,
                        LunaMetrics.CaptionButtonGradientLowerPosition,
                        1f
                    }
                };
                args.Graphics.FillPath(brush, path);
                args.Graphics.DrawPath(border, path);

                var inner = Rectangle.Inflate(bounds, -1, -1);
                using (var innerPath = XpDrawing.CreateRoundedRectangle(
                           inner,
                           LunaMetrics.CaptionButtonCornerRadius - 1))
                using (var innerHighlight = new Pen(XpPalette.LunaButtonInnerHighlight))
                {
                    args.Graphics.DrawPath(innerHighlight, innerPath);
                }
            }

            if (_pressed)
            {
                args.Graphics.TranslateTransform(1f, 1f);
            }
            DrawGlyph(args.Graphics);
        }

        private void GetGradientColors(
            bool isClose,
            out Color top,
            out Color shoulder,
            out Color peak,
            out Color upper,
            out Color middle,
            out Color lower,
            out Color bottom)
        {
            if (!IsWindowActive)
            {
                top = XpPalette.LunaInactiveButtonTop;
                shoulder = XpPalette.LunaInactiveButtonTop;
                peak = XpPalette.LunaInactiveButtonUpper;
                upper = XpPalette.LunaInactiveButtonUpper;
                middle = XpPalette.LunaInactiveButtonLower;
                lower = XpPalette.LunaInactiveButtonLower;
                bottom = XpPalette.LunaInactiveButtonBottom;
                return;
            }

            if (isClose)
            {
                top = _hovered ? XpPalette.LunaCloseHoverUpper : XpPalette.LunaCloseTop;
                shoulder = top;
                peak = _hovered ? XpPalette.LunaCloseHoverTop : XpPalette.LunaClosePeak;
                upper = _hovered ? XpPalette.LunaCloseHoverUpper : XpPalette.LunaCloseUpper;
                middle = _hovered ? XpPalette.LunaCloseHoverLower : XpPalette.LunaCloseMiddle;
                lower = _hovered ? XpPalette.LunaCloseHoverLower : XpPalette.LunaCloseLower;
                bottom = _hovered ? XpPalette.LunaCloseHoverBottom : XpPalette.LunaCloseBottom;
                return;
            }

            top = _hovered ? XpPalette.LunaButtonHoverUpper : XpPalette.LunaButtonTop;
            shoulder = top;
            peak = _hovered ? XpPalette.LunaButtonHoverTop : XpPalette.LunaButtonPeak;
            upper = _hovered ? XpPalette.LunaButtonHoverUpper : XpPalette.LunaButtonUpper;
            middle = _hovered ? XpPalette.LunaButtonHoverLower : XpPalette.LunaButtonMiddle;
            lower = _hovered ? XpPalette.LunaButtonHoverLower : XpPalette.LunaButtonLower;
            bottom = _hovered ? XpPalette.LunaButtonHoverBottom : XpPalette.LunaButtonBottom;
        }

        private static void Swap(ref Color first, ref Color second)
        {
            var value = first;
            first = second;
            second = value;
        }

        private void DrawGlyph(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.None;
            var isClose = _kind == LunaCaptionButtonKind.Close;
            var shadowColor = isClose ? XpPalette.LunaCloseGlyphShadow : XpPalette.LunaGlyphShadow;
            var shadowWidth = isClose
                ? LunaMetrics.CaptionCloseGlyphShadowWidth
                : LunaMetrics.CaptionGlyphShadowWidth;
            var foregroundWidth = isClose
                ? LunaMetrics.CaptionCloseGlyphWidth
                : LunaMetrics.CaptionGlyphWidth;
            using (var shadow = new Pen(shadowColor, shadowWidth))
            using (var foreground = new Pen(
                       IsWindowActive ? XpPalette.ControlLightLight : XpPalette.LunaInactiveGlyph,
                       foregroundWidth))
            {
                var centerX = Width / 2;
                var centerY = Height / 2;
                switch (_kind)
                {
                    case LunaCaptionButtonKind.Minimize:
                        graphics.DrawLine(
                            shadow,
                            centerX - LunaMetrics.CaptionGlyphHalfWidth,
                            centerY + LunaMetrics.CaptionGlyphMinimizeOffsetY + 1,
                            centerX + LunaMetrics.CaptionGlyphHalfWidth,
                            centerY + LunaMetrics.CaptionGlyphMinimizeOffsetY + 1);
                        graphics.DrawLine(
                            foreground,
                            centerX - LunaMetrics.CaptionGlyphHalfWidth,
                            centerY + LunaMetrics.CaptionGlyphMinimizeOffsetY,
                            centerX + LunaMetrics.CaptionGlyphHalfWidth,
                            centerY + LunaMetrics.CaptionGlyphMinimizeOffsetY);
                        break;
                    case LunaCaptionButtonKind.Maximize:
                        if (DrawRestoreGlyph)
                        {
                            var restoreWidth = LunaMetrics.CaptionGlyphRestoreWidth;
                            var restoreHeight = LunaMetrics.CaptionGlyphRestoreHeight;
                            graphics.DrawRectangle(shadow, centerX - 2, centerY - 5, restoreWidth, restoreHeight);
                            graphics.DrawRectangle(foreground, centerX - 2, centerY - 5, restoreWidth, restoreHeight);
                            graphics.DrawRectangle(shadow, centerX - 5, centerY - 2, restoreWidth, restoreHeight);
                            graphics.DrawRectangle(foreground, centerX - 5, centerY - 2, restoreWidth, restoreHeight);
                        }
                        else
                        {
                            var maximizeLeft = centerX - (LunaMetrics.CaptionGlyphMaximizeWidth / 2);
                            var maximizeTop = centerY - (LunaMetrics.CaptionGlyphMaximizeHeight / 2);
                            graphics.DrawRectangle(
                                shadow,
                                maximizeLeft,
                                maximizeTop,
                                LunaMetrics.CaptionGlyphMaximizeWidth,
                                LunaMetrics.CaptionGlyphMaximizeHeight);
                            graphics.DrawRectangle(
                                foreground,
                                maximizeLeft,
                                maximizeTop,
                                LunaMetrics.CaptionGlyphMaximizeWidth,
                                LunaMetrics.CaptionGlyphMaximizeHeight);
                            graphics.DrawLine(
                                foreground,
                                maximizeLeft + 1,
                                maximizeTop + 2,
                                maximizeLeft + LunaMetrics.CaptionGlyphMaximizeWidth - 1,
                                maximizeTop + 2);
                        }
                        break;
                    case LunaCaptionButtonKind.Close:
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        var halfWidth = LunaMetrics.CaptionGlyphHalfWidth;
                        graphics.DrawLine(shadow, centerX - halfWidth, centerY - halfWidth, centerX + halfWidth, centerY + halfWidth);
                        graphics.DrawLine(shadow, centerX + halfWidth, centerY - halfWidth, centerX - halfWidth, centerY + halfWidth);
                        graphics.DrawLine(foreground, centerX - halfWidth, centerY - halfWidth, centerX + halfWidth, centerY + halfWidth);
                        graphics.DrawLine(foreground, centerX + halfWidth, centerY - halfWidth, centerX - halfWidth, centerY + halfWidth);
                        break;
                }
            }
        }

    }
}
