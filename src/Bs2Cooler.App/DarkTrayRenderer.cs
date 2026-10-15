using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Bs2Cooler.App;

internal sealed class DarkTrayRenderer : Forms.ToolStripProfessionalRenderer
{
    public DarkTrayRenderer() : base(new Palette()) { RoundedEdges = false; }
    protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Drawing.Color.FromArgb(237, 244, 255) : Drawing.Color.FromArgb(166, 181, 202);
        base.OnRenderItemText(e);
    }
    protected override void OnRenderItemCheck(Forms.ToolStripItemImageRenderEventArgs e)
    {
        var rect = e.ImageRectangle;
        using var pen = new Drawing.Pen(Drawing.Color.FromArgb(85, 203, 176), 2);
        e.Graphics.DrawLines(pen, new Drawing.PointF[] { new(rect.Left + rect.Width * 0.15f, rect.Top + rect.Height * 0.5f),
            new(rect.Left + rect.Width * 0.4f, rect.Top + rect.Height * 0.75f), new(rect.Left + rect.Width * 0.85f, rect.Top + rect.Height * 0.2f) });
    }
    private sealed class Palette : Forms.ProfessionalColorTable
    {
        private static readonly Drawing.Color Background = Drawing.Color.FromArgb(24, 34, 49);
        private static readonly Drawing.Color Hover = Drawing.Color.FromArgb(42, 59, 82);
        private static readonly Drawing.Color Stroke = Drawing.Color.FromArgb(60, 80, 105);
        public Palette() { UseSystemColors = false; }
        public override Drawing.Color ToolStripDropDownBackground => Background;
        public override Drawing.Color MenuBorder => Stroke;
        public override Drawing.Color MenuItemBorder => Drawing.Color.FromArgb(85, 203, 176);
        public override Drawing.Color MenuItemSelected => Hover;
        public override Drawing.Color MenuItemSelectedGradientBegin => Hover;
        public override Drawing.Color MenuItemSelectedGradientEnd => Hover;
        public override Drawing.Color ImageMarginGradientBegin => Background;
        public override Drawing.Color ImageMarginGradientMiddle => Background;
        public override Drawing.Color ImageMarginGradientEnd => Background;
        public override Drawing.Color CheckBackground => Hover;
        public override Drawing.Color CheckSelectedBackground => Hover;
        public override Drawing.Color CheckPressedBackground => Hover;
        public override Drawing.Color SeparatorDark => Stroke;
        public override Drawing.Color SeparatorLight => Stroke;
    }
}
