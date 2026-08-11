using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal sealed class XpProfessionalColorTable : ProfessionalColorTable
    {
        internal XpProfessionalColorTable()
        {
            UseSystemColors = false;
        }

        public override Color ToolStripGradientBegin => XpPalette.ToolbarLight;
        public override Color ToolStripGradientMiddle => XpPalette.ToolbarLight;
        public override Color ToolStripGradientEnd => XpPalette.ToolbarDark;
        public override Color MenuStripGradientBegin => XpPalette.ToolbarLight;
        public override Color MenuStripGradientEnd => XpPalette.ToolbarDark;
        public override Color ToolStripBorder => XpPalette.ToolbarBorder;
        public override Color MenuBorder => XpPalette.ToolbarBorder;
        public override Color MenuItemBorder => XpPalette.MenuItemBorder;
        public override Color MenuItemSelected => XpPalette.SelectionLight;
        public override Color MenuItemSelectedGradientBegin => XpPalette.SelectionLight;
        public override Color MenuItemSelectedGradientEnd => XpPalette.SelectionDark;
        public override Color ButtonSelectedGradientBegin => XpPalette.SelectionLight;
        public override Color ButtonSelectedGradientMiddle => XpPalette.SelectionLight;
        public override Color ButtonSelectedGradientEnd => XpPalette.SelectionDark;
        public override Color ButtonPressedGradientBegin => XpPalette.SelectionDark;
        public override Color ButtonPressedGradientMiddle => XpPalette.SelectionLight;
        public override Color ButtonPressedGradientEnd => XpPalette.SelectionDark;
        public override Color ImageMarginGradientBegin => XpPalette.ToolbarLight;
        public override Color ImageMarginGradientMiddle => XpPalette.ToolbarLight;
        public override Color ImageMarginGradientEnd => XpPalette.ToolbarDark;
        public override Color SeparatorDark => XpPalette.ToolbarBorder;
        public override Color SeparatorLight => XpPalette.ControlLightLight;
    }

    internal sealed class XpToolStripRenderer : ToolStripProfessionalRenderer
    {
        internal XpToolStripRenderer() : base(new XpProfessionalColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (var border = new Pen(XpPalette.ControlDark))
                using (var inner = new Pen(XpPalette.ControlLightLight))
                {
                    e.Graphics.DrawRectangle(border, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                    e.Graphics.DrawRectangle(inner, 1, 1, e.ToolStrip.Width - 3, e.ToolStrip.Height - 3);
                }
                return;
            }

            var topColor = e.ToolStrip is MenuStrip
                ? XpPalette.MenuBarFace
                : XpPalette.ToolbarInnerBorder;
            using (var top = new Pen(topColor))
            using (var bottom = new Pen(XpPalette.ToolbarBorder))
            {
                e.Graphics.DrawLine(top, 0, 0, e.AffectedBounds.Right, 0);
                e.Graphics.DrawLine(
                    bottom,
                    0,
                    e.AffectedBounds.Bottom - 1,
                    e.AffectedBounds.Right,
                    e.AffectedBounds.Bottom - 1);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                e.Graphics.Clear(XpPalette.ControlFace);
                return;
            }

            if (e.ToolStrip is MenuStrip)
            {
                e.Graphics.Clear(XpPalette.MenuBarFace);
                return;
            }

            var bounds = e.AffectedBounds;
            using (var fill = new LinearGradientBrush(
                       bounds,
                       XpPalette.ToolbarLight,
                       XpPalette.ToolbarDark,
                       LinearGradientMode.Vertical))
            {
                fill.InterpolationColors = new ColorBlend
                {
                    Colors = new[]
                    {
                        XpPalette.ToolbarLight,
                        XpPalette.ToolbarMiddle,
                        XpPalette.ToolbarDark
                    },
                    Positions = new[] { 0f, LunaMetrics.ToolbarGradientMiddlePosition, 1f }
                };
                e.Graphics.FillRectangle(fill, bounds);
            }
        }

        protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
        {
            var bounds = e.GripBounds;
            var horizontal = e.GripDisplayStyle == ToolStripGripDisplayStyle.Vertical;
            using (var shadow = new SolidBrush(XpPalette.GripShadow))
            using (var highlight = new SolidBrush(XpPalette.GripHighlight))
            {
                if (horizontal)
                {
                    var x = bounds.Left + (bounds.Width / 2) - 1;
                    for (var y = bounds.Top + 3; y < bounds.Bottom - 2; y += 4)
                    {
                        e.Graphics.FillRectangle(shadow, x, y, 2, 2);
                        e.Graphics.FillRectangle(highlight, x + 1, y + 1, 1, 1);
                    }
                }
                else
                {
                    var y = bounds.Top + (bounds.Height / 2) - 1;
                    for (var x = bounds.Left + 3; x < bounds.Right - 2; x += 4)
                    {
                        e.Graphics.FillRectangle(shadow, x, y, 2, 2);
                        e.Graphics.FillRectangle(highlight, x + 1, y + 1, 1, 1);
                    }
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var shadow = new Pen(XpPalette.ToolbarBorder))
            using (var highlight = new Pen(XpPalette.ControlLightLight))
            {
                if (e.Vertical)
                {
                    var x = e.Item.Width / 2;
                    e.Graphics.DrawLine(shadow, x, 3, x, e.Item.Height - 4);
                    e.Graphics.DrawLine(highlight, x + 1, 3, x + 1, e.Item.Height - 4);
                }
                else
                {
                    var y = e.Item.Height / 2;
                    e.Graphics.DrawLine(shadow, 3, y, e.Item.Width - 4, y);
                    e.Graphics.DrawLine(highlight, 3, y + 1, e.Item.Width - 4, y + 1);
                }
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var pressed = e.Item is ToolStripButton button && (button.Pressed || button.Checked);
            if (e.Item.Selected || pressed)
            {
                DrawSelection(e.Graphics, e.Item.ContentRectangle, pressed);
            }
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var pressed = e.Item is ToolStripDropDownItem item && item.DropDown.Visible;
            if (e.Item.Selected || pressed)
            {
                DrawSelection(e.Graphics, e.Item.ContentRectangle, pressed);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var pressed = e.Item is ToolStripMenuItem item && item.DropDown.Visible;
            if (e.Item.Selected || pressed)
            {
                var bounds = new Rectangle(1, 1, e.Item.Width - 3, e.Item.Height - 3);
                DrawSelection(e.Graphics, bounds, pressed);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var fill = new LinearGradientBrush(
                       e.AffectedBounds,
                       XpPalette.ToolbarLight,
                       XpPalette.ToolbarDark,
                       LinearGradientMode.Horizontal))
            using (var separator = new Pen(XpPalette.ToolbarBorder))
            {
                e.Graphics.FillRectangle(fill, e.AffectedBounds);
                e.Graphics.DrawLine(
                    separator,
                    e.AffectedBounds.Right - 1,
                    e.AffectedBounds.Top,
                    e.AffectedBounds.Right - 1,
                    e.AffectedBounds.Bottom);
            }
        }

        private static void DrawSelection(Graphics graphics, Rectangle bounds, bool pressed)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var adjusted = Rectangle.Inflate(bounds, -1, -1);
            var top = pressed ? XpPalette.SelectionDark : XpPalette.SelectionLight;
            var bottom = pressed ? XpPalette.SelectionLight : XpPalette.SelectionDark;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = XpDrawing.CreateRoundedRectangle(adjusted, LunaMetrics.ToolbarItemCornerRadius))
            using (var fill = new LinearGradientBrush(adjusted, top, bottom, LinearGradientMode.Vertical))
            using (var border = new Pen(XpPalette.MenuItemBorder))
            {
                graphics.FillPath(fill, path);
                graphics.DrawPath(border, path);
            }
            graphics.SmoothingMode = SmoothingMode.None;
        }
    }
}
