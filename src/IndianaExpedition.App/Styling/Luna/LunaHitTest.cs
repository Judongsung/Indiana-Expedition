using System.Drawing;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal enum LunaHitTarget
    {
        Client = 1,
        Left = 10,
        Right = 11,
        Top = 12,
        TopLeft = 13,
        TopRight = 14,
        Bottom = 15,
        BottomLeft = 16,
        BottomRight = 17
    }

    internal static class LunaHitTest
    {
        internal static LunaHitTarget CalculateResizeTarget(Point point, Size clientSize)
        {
            var left = point.X < LunaMetrics.ResizeGripThickness;
            var right = point.X >= clientSize.Width - LunaMetrics.ResizeGripThickness;
            var top = point.Y < LunaMetrics.ResizeGripThickness;
            var bottom = point.Y >= clientSize.Height - LunaMetrics.ResizeGripThickness;

            if (left && top) return LunaHitTarget.TopLeft;
            if (right && top) return LunaHitTarget.TopRight;
            if (left && bottom) return LunaHitTarget.BottomLeft;
            if (right && bottom) return LunaHitTarget.BottomRight;
            if (left) return LunaHitTarget.Left;
            if (right) return LunaHitTarget.Right;
            if (top) return LunaHitTarget.Top;
            if (bottom) return LunaHitTarget.Bottom;
            return LunaHitTarget.Client;
        }
    }
}
