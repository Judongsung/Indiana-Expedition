using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IndianaExpedition.Styling
{
    internal static class XpDrawing
    {
        internal static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            var safeRadius = System.Math.Max(
                1,
                System.Math.Min(radius, System.Math.Min(bounds.Width, bounds.Height) / 2));
            var diameter = safeRadius * 2;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static GraphicsPath CreateTopRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            var safeRadius = System.Math.Max(
                1,
                System.Math.Min(radius, System.Math.Min(bounds.Width, bounds.Height) / 2));
            var diameter = safeRadius * 2;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddLine(bounds.Right, bounds.Bottom, bounds.Left, bounds.Bottom);
            path.CloseFigure();
            return path;
        }

        internal static TextFormatFlags GetTextFormatFlags(ContentAlignment alignment, bool showKeyboardCues)
        {
            var flags = TextFormatFlags.SingleLine |
                        TextFormatFlags.EndEllipsis |
                        TextFormatFlags.VerticalCenter;

            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    flags |= TextFormatFlags.Left;
                    break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    flags |= TextFormatFlags.Right;
                    break;
                default:
                    flags |= TextFormatFlags.HorizontalCenter;
                    break;
            }

            if (!showKeyboardCues)
            {
                flags |= TextFormatFlags.HidePrefix;
            }
            return flags;
        }

        internal static Rectangle AlignRectangle(Size size, Rectangle bounds, ContentAlignment alignment)
        {
            var x = alignment == ContentAlignment.TopRight ||
                    alignment == ContentAlignment.MiddleRight ||
                    alignment == ContentAlignment.BottomRight
                ? bounds.Right - size.Width
                : alignment == ContentAlignment.TopCenter ||
                  alignment == ContentAlignment.MiddleCenter ||
                  alignment == ContentAlignment.BottomCenter
                    ? bounds.Left + ((bounds.Width - size.Width) / 2)
                    : bounds.Left;

            var y = alignment == ContentAlignment.BottomLeft ||
                    alignment == ContentAlignment.BottomCenter ||
                    alignment == ContentAlignment.BottomRight
                ? bounds.Bottom - size.Height
                : alignment == ContentAlignment.MiddleLeft ||
                  alignment == ContentAlignment.MiddleCenter ||
                  alignment == ContentAlignment.MiddleRight
                    ? bounds.Top + ((bounds.Height - size.Height) / 2)
                    : bounds.Top;
            return new Rectangle(x, y, size.Width, size.Height);
        }
    }
}
