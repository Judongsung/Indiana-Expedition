using System;
using System.Drawing;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal sealed class LunaCaptionLayout
    {
        internal LunaCaptionLayout(
            Rectangle minimizeButtonBounds,
            Rectangle maximizeButtonBounds,
            Rectangle closeButtonBounds,
            Rectangle iconBounds,
            Rectangle textBounds)
        {
            MinimizeButtonBounds = minimizeButtonBounds;
            MaximizeButtonBounds = maximizeButtonBounds;
            CloseButtonBounds = closeButtonBounds;
            IconBounds = iconBounds;
            TextBounds = textBounds;
        }

        internal Rectangle MinimizeButtonBounds { get; }

        internal Rectangle MaximizeButtonBounds { get; }

        internal Rectangle CloseButtonBounds { get; }

        internal Rectangle IconBounds { get; }

        internal Rectangle TextBounds { get; }
    }

    internal static class LunaLayout
    {
        internal static LunaCaptionLayout CalculateCaption(
            Size clientSize,
            bool showIcon,
            bool showMinimize,
            bool showMaximize)
        {
            var right = Math.Max(0, clientSize.Width - LunaMetrics.CaptionRightPadding);
            var closeBounds = PlaceCaptionButton(ref right, LunaMetrics.CaptionCloseButtonWidth);
            var maximizeBounds = showMaximize
                ? PlaceCaptionButton(ref right, LunaMetrics.CaptionButtonWidth)
                : Rectangle.Empty;
            var minimizeBounds = showMinimize
                ? PlaceCaptionButton(ref right, LunaMetrics.CaptionButtonWidth)
                : Rectangle.Empty;

            var firstButtonLeft = showMinimize
                ? minimizeBounds.Left
                : showMaximize
                    ? maximizeBounds.Left
                    : closeBounds.Left;

            var textLeft = LunaMetrics.CaptionIconLeft;
            var iconBounds = Rectangle.Empty;
            if (showIcon)
            {
                iconBounds = new Rectangle(
                    LunaMetrics.CaptionIconLeft,
                    Math.Max(0, (clientSize.Height - LunaMetrics.CaptionIconSize) / 2),
                    LunaMetrics.CaptionIconSize,
                    LunaMetrics.CaptionIconSize);
                textLeft = iconBounds.Right + LunaMetrics.CaptionTextGap;
            }

            var textRight = Math.Max(textLeft, firstButtonLeft - LunaMetrics.CaptionTextGap);
            var textBounds = new Rectangle(
                textLeft,
                0,
                Math.Max(0, textRight - textLeft),
                Math.Max(0, clientSize.Height - 1));

            return new LunaCaptionLayout(
                minimizeBounds,
                maximizeBounds,
                closeBounds,
                iconBounds,
                textBounds);
        }

        private static Rectangle PlaceCaptionButton(ref int right, int width)
        {
            right -= width;
            var bounds = new Rectangle(
                right,
                LunaMetrics.CaptionTopPadding,
                width,
                LunaMetrics.CaptionButtonHeight);
            right -= LunaMetrics.CaptionButtonSpacing;
            return bounds;
        }
    }
}
