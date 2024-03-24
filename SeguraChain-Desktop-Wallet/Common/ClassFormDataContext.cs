using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.Common
{
    /// <summary>Contexto de dato de un formulario (Asociar a la propiedad Tag del formulario)</summary>
    public class ClassDataContextForm
    {
        /// <summary>Define la estrategia actual escogida en el formulario al que aplica</summary>
        public ClassViewStrategiesEnum ActualStretegyView { get; set; }

        /// <summary>Datos Iniciales del Diseño Inicial del forumulario</summary>
        public ClassContainerResponsive FormContainerResponsiveData { get; set; }

        /// <summary>
        /// Obtiene los datos iniciales más representativos de los controles de un formulario para volver a restaurarlo en el 
        /// cambio de posición y tamaño de los controles en las diferentes vistas que se ofrecen
        /// </summary>
        /// <param name="f1">Formulario</param>
        public void InitDataResponsiveFormControls(Form f1)
        {
            if (f1.Controls != null && f1.Controls.Count > 0)
            {
                FormContainerResponsiveData = new ClassContainerResponsive();
                List<ClassContainerDataLocalization> lCs = new List<ClassContainerDataLocalization>();
                recursiveSearchControlsChilds(f1, lCs, null);
                FormContainerResponsiveData.ControlsCompData = lCs;
            }
        }

        /// <summary>
        /// Busca Controles en un formulario y los controles dentro de los controles de los controles... de ese formulario
        /// </summary>
        /// <param name="f1">Formulario</param>
        /// <param name="lCD">Lista de Controles</param>
        /// <param name="cD_BF">Control Padre en la iteración actual de sus hijos</param>
        private static void recursiveSearchControlsChilds(Form f1, List<ClassContainerDataLocalization> lCD, Control cD_BF)
        {
            // Sólo con sentido
            foreach (Control c in cD_BF == null ? f1.Controls : cD_BF.Controls)
            {
                // Obtenemos los datos iniciales
                ClassContainerDataLocalization cD =
                    new ClassContainerDataLocalization()
                    {
                        InitFormHeight = f1.Height,
                        InitFormWidth = f1.Width,
                        Control = c,
                        InitX = c.Location.X,
                        InitY = c.Location.Y,
                        InitHeight = c.Height,
                        InitWidth = c.Width,
                        Dock = c.Dock,
                        Anchor = c.Anchor
                    };
                lCD.Add(cD);

                if (c.Controls != null && c.Controls.Count > 0)
                {
                    // Seguimos la madriguera de conejos > | > | > | O
                    cD.ChildsContainerData = new List<ClassContainerDataLocalization>();
                    recursiveSearchControlsChilds(f1, cD.ChildsContainerData, c);
                }
            }
        }
    }
}
