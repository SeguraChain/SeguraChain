using SeguraChain_Desktop_Wallet.Components;
using System.Drawing;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.InternalForm.Custom.Object
{
    class ClassCustomProgressBar : ProgressBar
    {
        private bool _roundedEdgesDrawed = false;

        public ClassCustomProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
        }


        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x000F)
            {
                var flags = TextFormatFlags.VerticalCenter |
                            TextFormatFlags.HorizontalCenter |
                            TextFormatFlags.SingleLine |
                            TextFormatFlags.WordEllipsis;
                using (Graphics progressBarGraphics = CreateGraphics())
                {
                    double percent = Value / 100d;

                    double percentWidth = (Width * percent) / 100d;

                    progressBarGraphics.FillRectangle(new SolidBrush(Color.Green), new Rectangle(0, 0, (int)percentWidth, Height));


                    TextRenderer.DrawText(progressBarGraphics,
                        percent + "%",
                        Font,
                        new Rectangle(0, 0, Width, Height),
                        Color.Black,
                        flags);

                    if (!_roundedEdgesDrawed)
                    {
                        ClassGraphicsUtility.DrawControlRoundedEdges(this, progressBarGraphics, 10, 0.5f, Color.Green);
                        _roundedEdgesDrawed = true;
                    }
                }
            }
        }
    }
}
