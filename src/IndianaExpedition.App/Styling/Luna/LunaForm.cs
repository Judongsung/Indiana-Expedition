using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IndianaExpedition.Constants;

namespace IndianaExpedition.Styling
{
    internal class LunaForm : Form
    {
        private const int WindowMessageNonClientHitTest = 0x0084;
        private const int WindowMessageGetMinMaxInfo = 0x0024;
        private const int WindowMessageNonClientLeftButtonDown = 0x00A1;
        private const int HitTestClient = 1;
        private const int HitTestCaption = 2;
        private const int MonitorDefaultToNearest = 2;
        private const int DropShadowClassStyle = 0x00020000;
        private const int NoActivateExtendedStyle = 0x08000000;

        private readonly LunaTitleBar _titleBar;
        private readonly Font _captionFont;
        private readonly Font _uiFont;
        private bool _lunaChromeVisible = true;
        private bool _lunaResizable = true;
        private bool _active = true;
        private bool _preventActivationOnShow;

        protected LunaForm()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            FormBorderStyle = FormBorderStyle.None;
            Padding = CreateChromePadding();
            BackColor = XpPalette.LunaFrameLeftOuter;

            _captionFont = new Font(
                LunaMetrics.CaptionFontName,
                LunaMetrics.CaptionFontSize,
                FontStyle.Bold,
                GraphicsUnit.Point);
            _uiFont = new Font(
                LunaMetrics.UiFontName,
                LunaMetrics.UiFontSize,
                FontStyle.Regular,
                GraphicsUnit.Point);
            Font = _uiFont;
            ContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = XpPalette.ControlFace,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _titleBar = new LunaTitleBar(this);

            Controls.Add(ContentPanel);
            Controls.Add(_titleBar);
        }

        protected Panel ContentPanel { get; }

        internal Font CaptionFont => _captionFont;

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ClassStyle |= DropShadowClassStyle;
                if (_preventActivationOnShow)
                {
                    parameters.ExStyle |= NoActivateExtendedStyle;
                }
                return parameters;
            }
        }

        protected override bool ShowWithoutActivation =>
            _preventActivationOnShow || base.ShowWithoutActivation;

        protected bool LunaResizable
        {
            get => _lunaResizable;
            set => _lunaResizable = value;
        }

        protected bool PreventActivationOnShow
        {
            get => _preventActivationOnShow;
            set => _preventActivationOnShow = value;
        }

        protected void SetContentClientSize(int width, int height)
        {
            ClientSize = new Size(
                width + (LunaMetrics.FrameSideThickness * 2),
                height + LunaMetrics.CaptionHeight + LunaMetrics.FrameBottomThickness);
        }

        protected void SetLunaChromeVisible(bool visible)
        {
            if (_lunaChromeVisible == visible)
            {
                return;
            }

            _lunaChromeVisible = visible;
            _titleBar.Visible = visible;
            Padding = visible ? CreateChromePadding() : Padding.Empty;
            BackColor = visible ? XpPalette.LunaFrameLeftOuter : ContentPanel.BackColor;
            UpdateWindowRegion();
            PerformLayout();
        }

        internal void BeginWindowDrag()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WindowMessageNonClientLeftButtonDown, (IntPtr)HitTestCaption, IntPtr.Zero);
        }

        internal void MinimizeWindow()
        {
            WindowState = FormWindowState.Minimized;
        }

        internal void ToggleMaximizeWindow()
        {
            if (!MaximizeBox)
            {
                return;
            }

            WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        protected override void OnActivated(EventArgs args)
        {
            _active = true;
            _titleBar.SetActive(true);
            Invalidate();
            base.OnActivated(args);
        }

        protected override void OnDeactivate(EventArgs args)
        {
            _active = false;
            _titleBar.SetActive(false);
            Invalidate();
            base.OnDeactivate(args);
        }

        protected override void OnPaintBackground(PaintEventArgs args)
        {
            if (!_lunaChromeVisible)
            {
                args.Graphics.Clear(ContentPanel.BackColor);
                return;
            }

            var bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var leftOuter = _active ? XpPalette.LunaFrameLeftOuter : XpPalette.LunaInactiveBorderOuter;
            var leftBase = _active ? XpPalette.LunaFrameLeftBase : XpPalette.LunaInactiveBorder;
            var leftHighlight = _active ? XpPalette.LunaFrameLeftHighlight : XpPalette.LunaInactiveBorderInner;
            var leftInner = _active ? XpPalette.LunaFrameLeftInner : XpPalette.LunaInactiveBorder;
            var rightInner = _active ? XpPalette.LunaFrameRightInner : XpPalette.LunaInactiveBorderInner;
            var rightMiddle = _active ? XpPalette.LunaFrameRightMiddle : XpPalette.LunaInactiveBorder;
            var rightShadow = _active ? XpPalette.LunaFrameRightShadow : XpPalette.LunaInactiveBorderOuter;
            var rightOuter = _active ? XpPalette.LunaFrameRightOuter : XpPalette.LunaInactiveBorderOuter;

            args.Graphics.Clear(leftBase);
            var outerBounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var outerPath = XpDrawing.CreateTopRoundedRectangle(outerBounds, LunaMetrics.CornerRadius))
            using (var outerPen = new Pen(leftOuter))
            using (var leftBasePen = new Pen(leftBase))
            using (var leftHighlightPen = new Pen(leftHighlight))
            using (var leftInnerPen = new Pen(leftInner))
            using (var rightInnerPen = new Pen(rightInner))
            using (var rightMiddlePen = new Pen(rightMiddle))
            using (var rightShadowPen = new Pen(rightShadow))
            using (var rightOuterPen = new Pen(rightOuter))
            {
                args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                args.Graphics.DrawPath(outerPen, outerPath);
                args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                var verticalStart = Math.Min(Height - 1, LunaMetrics.CornerRadius);
                var leftBaseX = 1;
                var leftHighlightX = 2;
                var leftInnerX = LunaMetrics.FrameSideThickness - 1;
                var rightInnerX = Width - LunaMetrics.FrameSideThickness;
                var rightMiddleX = rightInnerX + 1;
                var rightShadowX = rightInnerX + 2;
                var rightOuterX = Width - 1;
                args.Graphics.DrawLine(leftBasePen, leftBaseX, verticalStart, leftBaseX, Height - 1);
                args.Graphics.DrawLine(leftHighlightPen, leftHighlightX, verticalStart, leftHighlightX, Height - 1);
                args.Graphics.DrawLine(leftInnerPen, leftInnerX, verticalStart, leftInnerX, Height - 1);

                args.Graphics.DrawLine(rightInnerPen, rightInnerX, verticalStart, rightInnerX, Height - 1);
                args.Graphics.DrawLine(rightMiddlePen, rightMiddleX, verticalStart, rightMiddleX, Height - 1);
                args.Graphics.DrawLine(rightShadowPen, rightShadowX, verticalStart, rightShadowX, Height - 1);
                args.Graphics.DrawLine(rightOuterPen, rightOuterX, verticalStart, rightOuterX, Height - 1);

                var bottomInnerY = Height - LunaMetrics.FrameBottomThickness;
                var bottomMiddleY = bottomInnerY + 1;
                var bottomShadowY = bottomInnerY + 2;
                var bottomOuterY = Height - 1;
                args.Graphics.DrawLine(rightInnerPen, 0, bottomInnerY, Width - 1, bottomInnerY);
                args.Graphics.DrawLine(rightMiddlePen, 0, bottomMiddleY, Width - 1, bottomMiddleY);
                args.Graphics.DrawLine(rightShadowPen, 0, bottomShadowY, Width - 1, bottomShadowY);
                args.Graphics.DrawLine(rightOuterPen, 0, bottomOuterY, Width - 1, bottomOuterY);
            }
        }

        protected override void OnTextChanged(EventArgs args)
        {
            _titleBar?.Invalidate();
            base.OnTextChanged(args);
        }

        protected override void OnResize(EventArgs args)
        {
            base.OnResize(args);
            _titleBar?.RefreshWindowState();
            UpdateWindowRegion();
        }

        protected override void OnShown(EventArgs args)
        {
            _titleBar.RefreshWindowState();
            base.OnShown(args);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WindowMessageGetMinMaxInfo)
            {
                ApplyWorkingAreaBounds(message.LParam);
            }

            base.WndProc(ref message);

            if (message.Msg == WindowMessageNonClientHitTest &&
                (int)message.Result == HitTestClient &&
                _lunaChromeVisible &&
                _lunaResizable &&
                WindowState == FormWindowState.Normal)
            {
                var screenPoint = new Point(
                    unchecked((short)(long)message.LParam),
                    unchecked((short)((long)message.LParam >> 16)));
                var target = LunaHitTest.CalculateResizeTarget(PointToClient(screenPoint), ClientSize);
                message.Result = (IntPtr)(int)target;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _captionFont?.Dispose();
                _uiFont?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void UpdateWindowRegion()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            if (!_lunaChromeVisible || WindowState == FormWindowState.Maximized)
            {
                ReplaceWindowRegion(null);
                return;
            }

            var regionHandle = CreateRoundRectRgn(
                0,
                0,
                Width + 1,
                Height + 1,
                LunaMetrics.CornerRadius * 2,
                LunaMetrics.CornerRadius * 2);
            try
            {
                ReplaceWindowRegion(Region.FromHrgn(regionHandle));
            }
            finally
            {
                DeleteObject(regionHandle);
            }
        }

        private static Padding CreateChromePadding()
        {
            return new Padding(
                LunaMetrics.FrameSideThickness,
                0,
                LunaMetrics.FrameSideThickness,
                LunaMetrics.FrameBottomThickness);
        }

        private void ReplaceWindowRegion(Region replacement)
        {
            var previous = Region;
            Region = replacement;
            if (previous != null && !ReferenceEquals(previous, replacement))
            {
                previous.Dispose();
            }
        }

        private void ApplyWorkingAreaBounds(IntPtr lParam)
        {
            if (!IsHandleCreated)
            {
                return;
            }

            var monitor = MonitorFromWindow(Handle, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
            {
                return;
            }

            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            if (!GetMonitorInfo(monitor, ref monitorInfo))
            {
                return;
            }

            var minMax = (MinMaxInfo)Marshal.PtrToStructure(lParam, typeof(MinMaxInfo));
            minMax.MaxPosition.X = monitorInfo.WorkArea.Left - monitorInfo.MonitorArea.Left;
            minMax.MaxPosition.Y = monitorInfo.WorkArea.Top - monitorInfo.MonitorArea.Top;
            minMax.MaxSize.X = monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left;
            minMax.MaxSize.Y = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top;
            Marshal.StructureToPtr(minMax, lParam, true);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int left,
            int top,
            int right,
            int bottom,
            int ellipseWidth,
            int ellipseHeight);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            internal int X;
            internal int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            internal NativePoint Reserved;
            internal NativePoint MaxSize;
            internal NativePoint MaxPosition;
            internal NativePoint MinTrackSize;
            internal NativePoint MaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRectangle
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            internal int Size;
            internal NativeRectangle MonitorArea;
            internal NativeRectangle WorkArea;
            internal int Flags;
        }
    }
}
