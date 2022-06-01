using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.Components
{
    public class ClassGraphicsUtility
    {

        /// <summary>
        /// Auto set location of a control from another control source.
        /// </summary>
        /// <param name="controlTarget"></param>
        /// <param name="controlSource"></param>
        /// <param name="percentPositionX"></param>
        /// <param name="autoSize"></param>
        /// <returns></returns>
        public static T AutoSetLocationAndResizeControl<T>(Control controlTarget, Control controlSource, double percentPositionX, bool autoSize)
        {
            double textWidth = controlTarget.Width;

            textWidth = (textWidth * percentPositionX) / 100d;

            int positionX;
            if (autoSize)
            {
                controlTarget.Width = (int)textWidth;
                positionX = (int)(((controlSource.Width * percentPositionX) / 100d) - (textWidth / 2));

            }
            else
                positionX = (int)(((controlSource.Width * percentPositionX) / 100d) - textWidth);

            controlTarget.Location = new Point(positionX, controlTarget.Location.Y);

            return (T)Convert.ChangeType(controlTarget, typeof(T));
        }

        /// <summary>
        /// Auto resize a control from his text size, usefull for buttons.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <returns></returns>
        public static T AutoResizeControlFromText<T>(Control control)
        {
            using (Graphics cg = control.CreateGraphics())
            {
                SizeF size = cg.MeasureString(control.Text, control.Font);

                control.Width = (int)size.Width*2;

            }
            return (T)Convert.ChangeType(control, typeof(T));
        }

        /// <summary>
        /// Draw border on a control.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="borderColor"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="borderSize"></param>
        public static void DrawBorderOnControl(Graphics graphics, Color borderColor, int width, int height, float borderSize)
        {
            graphics.DrawRectangle(new Pen(borderColor, borderSize), 0, 0, width - (borderSize /2), height - (borderSize/2));
        }

        /// <summary>
        /// Draw shadow a list of controls from a main control and his graphics draw object target.
        /// </summary>
        /// <param name="mainControl"></param>
        /// <param name="listControlShadowTarget"></param>
        /// <param name="graphicTarget"></param>
        /// <param name="intensity"></param>
        /// <param name="radius"></param>
        /// <param name="bitmapShadow"></param>
        /// <param name="bitmapShadowUpdated"></param>
        public static void DrawShadowOnListGraphicContentTarget(Control mainControl, List<Control> listControlShadowTarget, Graphics graphicTarget, int intensity, int radius, Bitmap bitmapShadow, out Bitmap bitmapShadowUpdated)
        {
            if (bitmapShadow == null || bitmapShadow.Size != mainControl.Size)
            {
                bitmapShadow?.Dispose();
                bitmapShadow = new Bitmap(mainControl.Width, mainControl.Height, PixelFormat.Format32bppArgb);
            }

            foreach (Control control in listControlShadowTarget)
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddRectangle(new Rectangle(control.Location.X, control.Location.Y, control.Size.Width, control.Size.Height));
                    DrawShadowSmooth(gp, intensity, radius, bitmapShadow);
                }
                graphicTarget.DrawImage(bitmapShadow, new Point(0, 0));
            }

            bitmapShadowUpdated = bitmapShadow;
        }

        /// <summary>
        /// Draw shadow on a graphic bitmap target.
        /// </summary>
        /// <param name="gp"></param>
        /// <param name="intensity"></param>
        /// <param name="radius"></param>
        /// <param name="dest"></param>
        private static void DrawShadowSmooth(GraphicsPath gp, int intensity, int radius, Bitmap dest)
        {
            using (Graphics g = Graphics.FromImage(dest))
            {
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceCopy;
                double alpha = 0;
                double astep = 0;
                double astepstep = (double)intensity / radius / (radius / 2D);
                for (int thickness = radius; thickness > 0; thickness--)
                {
                    using (Pen p = new Pen(Color.FromArgb((int)alpha, 0, 0, 0), thickness))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, gp);
                    }
                    alpha += astep;
                    astep += astepstep;
                }
            }
        }

        /// <summary>
        /// Draw rounded edges on a control.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="graphics"></param>
        /// <param name="radius"></param>
        /// <param name="borderSize"></param>
        /// <param name="borderColor"></param>
        public static void DrawControlRoundedEdges(Control control, Graphics graphics, int radius, float borderSize, Color borderColor)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath graphicPath = new GraphicsPath())
            {
                graphicPath.StartFigure();
                graphicPath.AddArc(new Rectangle(0, 0, radius, radius), 180, 90);
                graphicPath.AddLine(radius, 0, control.Width - radius, 0);
                graphicPath.AddArc(new Rectangle(control.Width - radius, 0, radius, radius), 270, 90);
                graphicPath.AddLine(control.Width, radius, control.Width, control.Height - radius);
                graphicPath.AddArc(new Rectangle(control.Width - radius, control.Height - radius, radius, radius), 0, 90);
                graphicPath.AddLine(control.Width - radius, control.Height, radius, control.Height);
                graphicPath.AddArc(new Rectangle(0, control.Height - radius, radius, radius), 90, 90);
                graphicPath.AddLine(0, control.Height - radius, 0, radius);
                graphicPath.CloseFigure();
                control.Region = new Region(graphicPath);

                using (Pen _pen = new Pen(borderColor, borderSize))
                {
                    graphics.DrawArc(_pen, new Rectangle(0, 0, radius, radius), 180, 90);
                    graphics.DrawArc(_pen, new Rectangle(control.Width - radius - 1, -1, radius, radius), 270, 90);
                    graphics.DrawArc(_pen, new Rectangle(control.Width - radius - 1, control.Height - radius - 1, radius, radius), 0, 90);
                    graphics.DrawArc(_pen, new Rectangle(0, control.Height - radius - 1, radius, radius), 90, 90);
                    graphics.DrawRectangle(_pen, 0.0f, 0.0f, control.Width - 1.0f, control.Height - 1.0f);
                }
            }
        }

        /// <summary>
        /// Return a string measured in the middle position to draw with his width.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="positionXStart"></param>
        /// <param name="positionXEnd"></param>
        /// <param name="graphics"></param>
        /// <param name="font"></param>
        /// <param name="middlePosition"></param>
        /// <param name="widthText"></param>
        /// <returns></returns>
        public static string GetMeasureStringToDraw(string str, float positionXStart, int positionXEnd, Graphics graphics, Font font, float middlePosition, out float widthText)
        {
            #region Default values.

            string strFinal = string.Empty;
            widthText = 0;

            #endregion
            try
            {
                bool isAbove = false;

                foreach (char character in str)
                {
                    strFinal += character;

                    if (positionXStart + (graphics.MeasureString(strFinal, font).Width) > (positionXEnd - (middlePosition / 2)))
                    {
                        isAbove = true;
                        break;
                    }
                }

                if (isAbove)
                {
                    strFinal = strFinal.Substring(0, strFinal.Length - 3);
                    strFinal += "...";
                }

                widthText = graphics.MeasureString(strFinal, font).Width;
            }
#if DEBUG
            catch(Exception error)
            {
                Debug.WriteLine("Error to measure the string to draw: " + str +" | Exception: " + error.Message);
#else
            catch
            {
#endif

            }
            return strFinal;
        }

        /// <summary>
        /// Clone a bitmap.
        /// </summary>
        /// <param name="srcBitmap"></param>
        /// <returns></returns>
        public static Bitmap CloneBitmap(Bitmap bmpSrc)
        {
            Bitmap bmpDes = new Bitmap(bmpSrc.Width, bmpSrc.Height);
            if ((bmpSrc.Width == bmpDes.Width) && (bmpSrc.Height == bmpDes.Height) && (bmpSrc.PixelFormat == bmpDes.PixelFormat))
            {
                BitmapData bmpData;

                bmpData = bmpSrc.LockBits(new Rectangle(0, 0, bmpSrc.Width, bmpSrc.Height), ImageLockMode.ReadOnly, bmpSrc.PixelFormat);
                int lenght = bmpData.Stride * bmpData.Height;
                byte[] buffer = new byte[lenght];
                Marshal.Copy(bmpData.Scan0, buffer, 0, lenght);
                bmpSrc.UnlockBits(bmpData);

                bmpData = bmpDes.LockBits(new Rectangle(0, 0, bmpDes.Width, bmpDes.Height), ImageLockMode.WriteOnly, bmpDes.PixelFormat);
                Marshal.Copy(buffer, 0, bmpData.Scan0, lenght);
                bmpDes.UnlockBits(bmpData);
            }
            return bmpDes;

        }

    }
}
