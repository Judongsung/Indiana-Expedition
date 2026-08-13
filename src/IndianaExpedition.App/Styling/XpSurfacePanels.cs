using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal sealed class XpBandPanel : Panel
    {
        internal XpBandPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
        }

        protected override void OnPaintBackground(PaintEventArgs args)
        {
            var bounds = ClientRectangle;
            using (var brush = new LinearGradientBrush(
                       bounds,
                       XpPalette.ToolbarLight,
                       XpPalette.ToolbarDark,
                       LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend
                {
                    Colors = new[]
                    {
                        XpPalette.ToolbarLight,
                        XpPalette.ToolbarMiddle,
                        XpPalette.ToolbarDark
                    },
                    Positions = new[] { 0f, LunaMetrics.ToolbarGradientMiddlePosition, 1f }
                };
                brush.InterpolationColors = blend;
                args.Graphics.FillRectangle(brush, bounds);
            }

            using (var top = new Pen(XpPalette.ToolbarInnerBorder))
            using (var bottom = new Pen(XpPalette.ToolbarBorder))
            {
                args.Graphics.DrawLine(top, 0, 0, Width, 0);
                args.Graphics.DrawLine(bottom, 0, Height - 1, Width, Height - 1);
            }
        }
    }

    internal sealed class XpInformationBarPanel : Panel
    {
        internal XpInformationBarPanel()
        {
            BackColor = XpPalette.InformationBarFace;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
        }

        protected override void OnPaintBackground(PaintEventArgs args)
        {
            args.Graphics.Clear(XpPalette.InformationBarFace);
            using (var top = new Pen(XpPalette.ControlLightLight))
            using (var bottom = new Pen(XpPalette.InformationBarBorder))
            {
                args.Graphics.DrawLine(top, 0, 0, Width, 0);
                args.Graphics.DrawLine(bottom, 0, Height - 1, Width, Height - 1);
            }
        }
    }

    internal sealed class XpExplorerHeaderPanel : Panel
    {
        internal XpExplorerHeaderPanel()
        {
            BackColor = XpPalette.ExplorerHeaderMiddle;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
        }

        protected override void OnPaintBackground(PaintEventArgs args)
        {
            var bounds = ClientRectangle;
            using (var brush = new LinearGradientBrush(
                       bounds,
                       XpPalette.ExplorerHeaderStart,
                       XpPalette.ExplorerHeaderEnd,
                       LinearGradientMode.Horizontal))
            {
                var blend = new ColorBlend
                {
                    Colors = new[]
                    {
                        XpPalette.ExplorerHeaderStart,
                        XpPalette.ExplorerHeaderMiddle,
                        XpPalette.ExplorerHeaderEnd
                    },
                    Positions = new[] { 0f, 0.62f, 1f }
                };
                brush.InterpolationColors = blend;
                args.Graphics.FillRectangle(brush, bounds);
            }

            using (var highlight = new Pen(XpPalette.ExplorerHeaderHighlight))
            using (var shadow = new Pen(XpPalette.ExplorerHeaderShadow))
            {
                args.Graphics.DrawLine(highlight, 0, 0, Width, 0);
                args.Graphics.DrawLine(shadow, 0, Height - 1, Width, Height - 1);
            }
        }
    }

    internal sealed class XpExplorerHeaderLabel : Label
    {
        internal XpExplorerHeaderLabel()
        {
            BackColor = Color.Transparent;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint,
                true);
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            var textBounds = new Rectangle(
                Padding.Left,
                Padding.Top,
                Math.Max(0, Width - Padding.Horizontal),
                Math.Max(0, Height - Padding.Vertical));
            const TextFormatFlags flags = TextFormatFlags.Left |
                                          TextFormatFlags.VerticalCenter |
                                          TextFormatFlags.EndEllipsis |
                                          TextFormatFlags.NoPrefix |
                                          TextFormatFlags.SingleLine;
            var shadowBounds = textBounds;
            shadowBounds.Offset(1, 1);
            TextRenderer.DrawText(
                args.Graphics,
                Text,
                Font,
                shadowBounds,
                XpPalette.ExplorerHeaderTextShadow,
                flags);
            TextRenderer.DrawText(
                args.Graphics,
                Text,
                Font,
                textBounds,
                XpPalette.ExplorerHeaderText,
                flags);
        }
    }
}
