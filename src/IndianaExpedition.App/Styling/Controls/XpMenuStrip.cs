using System.Windows.Forms;

namespace IndianaExpedition.Styling
{
    internal sealed class XpMenuStrip : MenuStrip
    {
        private bool _suppressMouseUp;

        protected override void OnMouseDown(MouseEventArgs args)
        {
            _suppressMouseUp = false;

            var menuItem = GetItemAt(args.Location) as ToolStripMenuItem;
            if (TryCloseOpenDropDown(menuItem, args.Button))
            {
                return;
            }

            base.OnMouseDown(args);
        }

        protected override void OnMouseUp(MouseEventArgs args)
        {
            if (_suppressMouseUp)
            {
                _suppressMouseUp = false;
                return;
            }

            base.OnMouseUp(args);
        }

        internal bool TryCloseOpenDropDown(
            ToolStripMenuItem menuItem,
            MouseButtons mouseButton)
        {
            if (mouseButton != MouseButtons.Left || menuItem?.DropDown.Visible != true)
            {
                return false;
            }

            _suppressMouseUp = true;
            menuItem.HideDropDown();
            menuItem.Invalidate();
            return true;
        }
    }
}
