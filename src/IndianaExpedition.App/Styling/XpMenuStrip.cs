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
            if (args.Button == MouseButtons.Left && menuItem?.DropDown.Visible == true)
            {
                _suppressMouseUp = true;
                menuItem.HideDropDown();
                menuItem.Invalidate();
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
    }
}
