using SeguraChain_Desktop_Wallet.Enum;
using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.Components
{
    public class ClassGraphicsUtility
    {

        /// <summary>
        /// Move and adapt Containers or control with an strategy
        /// </summary>
        /// <param name="controls">Controles inmersos</param>
        /// <param name="f1">Formulario</param>        
        /// <param name="strategy"> 0 > WebSite Style > All CentereD in Single row</param>
        /// <param name="applyImages"> ¿Aplica también a imágenes?</param>
        public static void RecursiveAdaptResponsiveFormControlsToParentSize(List<ClassContainerDataLocalization> controls, Form f1, ClassViewStrategiesEnum strategy, Boolean applyImages)
        {
            if (controls != null && controls.Count > 0)
            {
                f1.AutoScrollPosition = new Point(0, 0);

                switch (strategy)
                {
                    case ClassViewStrategiesEnum.Normal:
                        //f1.AutoSize = true;
                        ViewStrategy_Normal(controls);

                        break;

                    case ClassViewStrategiesEnum.TypeWebSite:
                        //f1.AutoSize = true;
                        ViewStrategy_0_TypeWebSite(controls, f1.Width, applyImages);

                        break;

                    case ClassViewStrategiesEnum.LeftCenterRight:

                        ViewStrategy_1_LeftCenterRight(controls, f1.Width);

                        break;

                    case ClassViewStrategiesEnum.PorcentualDimensions:

                        float relPorcentualH = controls[0].InitFormHeight / f1.Height;
                        float relPorcentualW = controls[0].InitFormWidth / f1.Width;
                        ViewStrategy_2_PorcentualDimensions(controls, relPorcentualH, relPorcentualW);

                        break;
                }
            }
        }

        /// <summary>
        /// Estrategia que Organiza los controles de un formulario y sus componentes internos dentro del mismo
        /// de la forma en la que el diseñador establece, tal cual, sin cambios
        /// </summary>
        /// <param name="controls">Lista de Controles</param>
        private static void ViewStrategy_Normal(List<ClassContainerDataLocalization> controls)
        {
            foreach (ClassContainerDataLocalization c in controls)
            {
                c.Control.Width = c.InitWidth;
                c.Control.Height = c.InitHeight;
                c.Control.Location = new Point(c.InitX, c.InitY);

                if (c.HasChilds)
                {
                    ViewStrategy_Normal(c.ChildsContainerData);
                }
            }
        }

        /// <summary>
        /// Estrategia que Organiza los controles de un formulario y sus componentes internos dentro del mismo
        /// de la forma en la que el diseñador establece, tal cual, sin cambios
        /// </summary>
        /// <param name="controls">Lista de Controles</param>
        private static void ViewStrategy_2_PorcentualDimensions(List<ClassContainerDataLocalization> controls, float relPorcentualH, float relPorcentualW)
        {
            foreach (ClassContainerDataLocalization c in controls)
            {
                c.Control.Width = Convert.ToInt32(c.InitWidth * relPorcentualW);
                c.Control.Height = Convert.ToInt32(c.InitHeight * relPorcentualH);
                c.Control.Anchor = AnchorStyles.None;
                c.Control.Dock = DockStyle.None;
                c.Control.Location = new Point(Convert.ToInt32(c.InitX * relPorcentualW), Convert.ToInt32(c.InitY * relPorcentualH));

                if (c.HasChilds)
                {
                    ViewStrategy_2_PorcentualDimensions(c.ChildsContainerData, relPorcentualH, relPorcentualW);
                }
            }
        }

        /// <summary>
        /// Estrategia que Organiza los controles de un formulario y sus componentes internos dentro del mismo
        /// a la forma o estilo de una especie de página WeB, centrados y únicos en su línea o fila
        /// </summary>
        /// <param name="controls">Lista de Controles</param>
        /// <param name="newWidth">Ancho del contenedor</param>
        /// <param name="applyImages">Permite cambiar también las imágenes (true == Sí se cambian)</param>
        private static Int32 ViewStrategy_0_TypeWebSite(List<ClassContainerDataLocalization> controls, Int32 newWidth, Boolean applyImages)
        {
            // Centering
            Int32 centerY = 0;

            if (controls != null && controls.Count > 0)
            {
                Int32 centerX = newWidth / 2;

                List<ClassContainerDataLocalization> orderControls = controls.OrderBy(t => t.Control.TabIndex).ToList();

                // Prueba rápida y erróneamente de resultado inesperado
                foreach (ClassContainerDataLocalization c in orderControls)
                {
                    c.Control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    
                    //if (!applyImages)
                    //{
                    //    if ((String)c.Control.Tag != "image" && c.Control.GetType().Name != "PictureBox")
                    //    {
                    //        c.Control.Width = newWidth * 96 / 100;
                    //    }
                    //}
                    //else
                    //{
                    //    c.Control.Width = newWidth * 96 / 100;
                    //}

                    if (c.HasChilds)
                    {
                        ViewStrategy_0_TypeWebSite(c.ChildsContainerData, c.Control.Width, applyImages);
                    }

                    c.Control.Location = new Point(centerX - (c.Control.Width / 2), centerY);
                    centerY += c.Control.Height;
                }
            }
            return centerY;
        }

        /// <summary>
        /// Estrategia que Organiza los controles de un formulario y sus componentes internos dentro del mismo
        /// a la forma de ir despachando controles a la izquierda, centro o derecha, según les correspondan en su
        /// definición del formulario de diseño, teniendo en cuenta porcentuales y distancias iniciales para
        /// deliberar su posición al redimensionar la ventana que contiene el formulario
        /// </summary>
        /// <param name="controls">Lista de controles</param>
        /// <param name="newW">Ancho del contenedor</param>
        private static void ViewStrategy_1_LeftCenterRight(List<ClassContainerDataLocalization> controls, Int32 newW)
        {
            // Recorremos las líneas y las posicionamos en función de su posición antigua y nueva respecto a cuadrantes
            // y porcentuales para definir su destino final según las nuevas definicios de H/W
            if (controls != null && controls.Count > 0)
            {
                // Lefting                
                Int32 leftY = 24;
                Int32 leftX = 0;
                List<ClassContainerDataLocalization> leftActualControls = new List<ClassContainerDataLocalization>();

                // Centering                
                Int32 centerY = 24;
                Int32 centerX = newW / 2;

                // Rigthing                
                Int32 rigthY = 24;
                Int32 rightX = newW;
                List<ClassContainerDataLocalization> rightActualControls = new List<ClassContainerDataLocalization>();

                List<ClassContainerDataLocalization> orderControls = controls.OrderBy(t => t.Control.TabIndex).ToList();

                // Prueba rápida y erróneamente de resultado inesperado
                foreach (ClassContainerDataLocalization c in orderControls)
                {
                    // Un control que ocupa más del 80 % del tamaño de ancho debería ir al centro y no entraría nada
                    // a sus lados por lo que la componente Y sube en todos los cuadrantes
                    // HIGH WiDTH To MaX CENTER Y
                    if (c.InitWidth * 100 / c.InitFormWidth > 67)
                    {
                        centerX = newW / 2;

                        // Ajustamos al 96 de tamaño width del formulario
                        c.Control.Width = newW * 96 / 100;

                        if(leftActualControls.Count > 0)
                        {
                            leftY += leftActualControls.Max(m => m.Control.Height);
                        }

                        if(rightActualControls.Count > 0)
                        {
                            rigthY += rightActualControls.Max(m => m.Control.Height);
                        }

                        List<Int32> yLCR = new List<Int32>() { leftY, centerY, rigthY };

                        Int32 MaxAll = yLCR.Max(); // + c.Control.Margin.Top + c.Control.Padding.Top;

                        c.Control.Location = new Point(centerX - (c.Control.Width / 2), MaxAll);

                        Int32 UPPER_SET_ALL = MaxAll + c.Control.Height;
                        //+ c.Control.Margin.Bottom + c.Control.Padding.Bottom;

                        // SET Y
                        leftY = UPPER_SET_ALL;
                        centerY = UPPER_SET_ALL;
                        rigthY = UPPER_SET_ALL;

                        // SET X
                        leftX = 0;
                        centerX = newW / 2;
                        rightX = newW;
                    }
                    // TO LEFT
                    else if (c.InitX + c.InitWidth < c.InitFormWidth / 2)
                    {
                        //leftY += c.Control.Margin.Top + c.Control.Padding.Top;

                        if (leftX + c.Control.Width > newW / 3) // El Tercio, salud! xD
                        {
                            leftY += leftActualControls.Count > 0 ? leftActualControls.Max(m => m.Control.Height): 0;
                            leftX = 0;
                            leftActualControls.Clear();
                        }

                        leftActualControls.Add(c);

                        c.Control.Location = new Point(leftX, leftY);

                        // SET X
                        leftX += c.Control.Width;
                    }
                    // TO RIGHT
                    else if (c.InitX > c.InitFormWidth / 2)
                    {
                        rigthY += c.Control.Margin.Top + c.Control.Padding.Top;

                        if (rightX - c.Control.Width
                            < 2 * newW / 3) // El Tercio, salud! xD
                        {
                            rigthY += rightActualControls.Count > 0 ? rightActualControls.Max(m => m.Control.Height) : 0;
                            rightX = newW;
                            rightActualControls.Clear();
                        }

                        rightActualControls.Add(c);

                        // SET X
                        rightX -= c.Control.Width;

                        c.Control.Location = new Point(rightX, rigthY);
                    }
                    // TO NORMAL CENTER
                    else
                    {
                        centerX = newW / 2;
                        c.Control.Location = new Point(centerX - (c.Control.Width / 2), centerY);

                        // SET Y
                        centerY += c.Control.Width;
                    }

                    if (c.HasChilds)
                    {
                        ViewStrategy_1_LeftCenterRight(
                        c.ChildsContainerData, c.Control.Width);
                    }
                }

            }
        }

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
                if (graphics == null)
                    return strFinal;

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
