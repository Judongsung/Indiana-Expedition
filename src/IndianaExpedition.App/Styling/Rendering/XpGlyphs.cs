using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal enum GlyphKind
    {
        Back,
        Forward,
        Stop,
        Refresh,
        Home,
        Favorites,
        History,
        Go,
        Folder,
        Page,
        Globe
    }

    internal static class XpGlyphs
    {
        internal static Bitmap Create(GlyphKind kind, int size = 24)
        {
            var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.Clear(Color.Transparent);
                Draw(graphics, kind, size);
            }
            return bitmap;
        }

        internal static Icon CreateApplicationIcon()
        {
            using (var bitmap = Create(GlyphKind.Globe, 32))
            {
                var handle = bitmap.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(handle).Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        private static void Draw(Graphics graphics, GlyphKind kind, int size)
        {
            var scale = size / 24f;
            graphics.ScaleTransform(scale, scale);

            switch (kind)
            {
                case GlyphKind.Back:
                    DrawArrow(graphics, false);
                    break;
                case GlyphKind.Forward:
                    DrawArrow(graphics, true);
                    break;
                case GlyphKind.Stop:
                    DrawStop(graphics);
                    break;
                case GlyphKind.Refresh:
                    DrawRefresh(graphics);
                    break;
                case GlyphKind.Home:
                    DrawHome(graphics);
                    break;
                case GlyphKind.Favorites:
                    DrawStar(graphics);
                    break;
                case GlyphKind.History:
                    DrawHistory(graphics);
                    break;
                case GlyphKind.Go:
                    DrawGo(graphics);
                    break;
                case GlyphKind.Folder:
                    DrawFolder(graphics);
                    break;
                case GlyphKind.Page:
                    DrawPage(graphics);
                    break;
                default:
                    DrawGlobe(graphics);
                    break;
            }
        }

        private static void DrawArrow(Graphics graphics, bool forward)
        {
            var points = new[]
            {
                new PointF(4, 12), new PointF(12, 4), new PointF(12, 9),
                new PointF(20, 9), new PointF(20, 15), new PointF(12, 15), new PointF(12, 20)
            };
            if (forward)
            {
                for (var index = 0; index < points.Length; index++)
                {
                    points[index].X = 24 - points[index].X;
                }
            }

            using (var brush = new LinearGradientBrush(
                       new Rectangle(3, 3, 18, 18),
                       XpGlyphPalette.ArrowTop,
                       XpGlyphPalette.ArrowBottom,
                       90f))
            using (var pen = new Pen(XpGlyphPalette.ArrowBorder, 1.2f))
            {
                graphics.FillPolygon(brush, points);
                graphics.DrawPolygon(pen, points);
            }
        }

        private static void DrawStop(Graphics graphics)
        {
            using (var pen = new Pen(XpGlyphPalette.Stop, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var highlight = new Pen(XpGlyphPalette.StopHighlight, 1f))
            {
                graphics.DrawLine(pen, 6, 6, 18, 18);
                graphics.DrawLine(pen, 18, 6, 6, 18);
                graphics.DrawLine(highlight, 6, 5, 19, 18);
            }
        }

        private static void DrawRefresh(Graphics graphics)
        {
            using (var pen = new Pen(XpGlyphPalette.Refresh, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                graphics.DrawArc(pen, 5, 5, 14, 14, 35, 275);
            }
            using (var brush = new SolidBrush(XpGlyphPalette.Refresh))
            {
                graphics.FillPolygon(brush, new[] { new Point(16, 3), new Point(21, 8), new Point(14, 9) });
            }
        }

        private static void DrawHome(Graphics graphics)
        {
            using (var roof = new SolidBrush(XpGlyphPalette.HomeRoof))
            using (var wall = new SolidBrush(XpGlyphPalette.HomeWall))
            using (var outline = new Pen(XpGlyphPalette.HomeOutline, 1.2f))
            using (var door = new SolidBrush(XpGlyphPalette.HomeDoor))
            {
                var roofPoints = new[] { new Point(3, 11), new Point(12, 3), new Point(21, 11), new Point(19, 13), new Point(12, 7), new Point(5, 13) };
                graphics.FillPolygon(roof, roofPoints);
                graphics.DrawPolygon(outline, roofPoints);
                graphics.FillRectangle(wall, 6, 11, 12, 9);
                graphics.DrawRectangle(outline, 6, 11, 12, 9);
                graphics.FillRectangle(door, 10, 14, 4, 6);
            }
        }

        private static void DrawStar(Graphics graphics)
        {
            var points = new PointF[10];
            for (var index = 0; index < points.Length; index++)
            {
                var radius = index % 2 == 0 ? 9f : 4f;
                var angle = -Math.PI / 2 + index * Math.PI / 5;
                points[index] = new PointF(12 + radius * (float)Math.Cos(angle), 12 + radius * (float)Math.Sin(angle));
            }
            using (var brush = new LinearGradientBrush(
                       new Rectangle(3, 3, 18, 18),
                       XpGlyphPalette.StarTop,
                       XpGlyphPalette.StarBottom,
                       90f))
            using (var pen = new Pen(XpGlyphPalette.StarBorder, 1f))
            {
                graphics.FillPolygon(brush, points);
                graphics.DrawPolygon(pen, points);
            }
        }

        private static void DrawHistory(Graphics graphics)
        {
            using (var fill = new SolidBrush(XpGlyphPalette.HistoryFace))
            using (var outline = new Pen(XpGlyphPalette.HistoryOutline, 2f))
            using (var hand = new Pen(XpGlyphPalette.HistoryHands, 1.8f) { EndCap = LineCap.Round })
            {
                graphics.FillEllipse(fill, 4, 4, 16, 16);
                graphics.DrawEllipse(outline, 4, 4, 16, 16);
                graphics.DrawLine(hand, 12, 12, 12, 7);
                graphics.DrawLine(hand, 12, 12, 16, 14);
            }
        }

        private static void DrawGo(Graphics graphics)
        {
            using (var fill = new LinearGradientBrush(
                       new Rectangle(3, 3, 18, 18),
                       XpGlyphPalette.GoTop,
                       XpGlyphPalette.GoBottom,
                       90f))
            using (var outline = new Pen(XpGlyphPalette.GoBorder, 1f))
            using (var white = new Pen(XpGlyphPalette.GoArrow, 2.5f) { EndCap = LineCap.Round })
            {
                graphics.FillEllipse(fill, 3, 3, 18, 18);
                graphics.DrawEllipse(outline, 3, 3, 18, 18);
                graphics.DrawLine(white, 7, 12, 17, 12);
                graphics.DrawLine(white, 13, 8, 17, 12);
                graphics.DrawLine(white, 13, 16, 17, 12);
            }
        }

        private static void DrawFolder(Graphics graphics)
        {
            using (var brush = new LinearGradientBrush(
                       new Rectangle(3, 5, 18, 14),
                       XpGlyphPalette.FolderTop,
                       XpGlyphPalette.FolderBottom,
                       90f))
            using (var pen = new Pen(XpGlyphPalette.FolderBorder, 1f))
            {
                graphics.FillPolygon(brush, new[] { new Point(3, 7), new Point(9, 7), new Point(11, 5), new Point(20, 5), new Point(21, 19), new Point(3, 19) });
                graphics.DrawPolygon(pen, new[] { new Point(3, 7), new Point(9, 7), new Point(11, 5), new Point(20, 5), new Point(21, 19), new Point(3, 19) });
            }
        }

        private static void DrawPage(Graphics graphics)
        {
            using (var brush = new SolidBrush(XpGlyphPalette.PageFace))
            using (var pen = new Pen(XpGlyphPalette.PageBorder, 1f))
            using (var line = new Pen(XpGlyphPalette.PageLine, 1f))
            {
                graphics.FillRectangle(brush, 5, 3, 14, 18);
                graphics.DrawRectangle(pen, 5, 3, 14, 18);
                graphics.DrawLine(line, 8, 8, 16, 8);
                graphics.DrawLine(line, 8, 12, 16, 12);
                graphics.DrawLine(line, 8, 16, 14, 16);
            }
        }

        private static void DrawGlobe(Graphics graphics)
        {
            using (var fill = new LinearGradientBrush(
                       new Rectangle(3, 3, 18, 18),
                       XpGlyphPalette.GlobeTop,
                       XpGlyphPalette.GlobeBottom,
                       90f))
            using (var outline = new Pen(XpGlyphPalette.GlobeBorder, 1f))
            using (var grid = new Pen(XpGlyphPalette.GlobeGrid, 1f))
            using (var orbit = new Pen(XpGlyphPalette.GlobeOrbit, 2f))
            {
                graphics.FillEllipse(fill, 3, 3, 18, 18);
                graphics.DrawEllipse(outline, 3, 3, 18, 18);
                graphics.DrawEllipse(grid, 8, 3, 8, 18);
                graphics.DrawLine(grid, 4, 10, 20, 10);
                graphics.DrawArc(orbit, 1, 7, 22, 10, 185, 170);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
