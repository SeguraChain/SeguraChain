using SeguraChain_Desktop_Wallet.Components;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace SeguraChain_Desktop_Wallet.InternalForm.Custom.Object
{
    public class ClassCustomPanel : Panel
    {

        private bool drawed;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        private float _borderSize = 3;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float BorderSize
        {
            get
            {
                return _borderSize;
            }
            set
            {
                _borderSize = value;
            }
        }

        private Color _borderColor = Color.White;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get
            {
                return _borderColor;
            }
            set
            {
                _borderColor = value;
            }
        }

        private int _radius = 2;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        public ClassCustomPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!drawed)
            {
                ClassGraphicsUtility.DrawControlRoundedEdges(this, e.Graphics, Radius, BorderSize, BorderColor);
                drawed = true;
            }
            base.OnPaint(e);

        }
    }
}
