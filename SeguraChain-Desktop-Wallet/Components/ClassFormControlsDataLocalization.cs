using SeguraChain_Desktop_Wallet.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.Components
{
    /// <summary>Añadir al formulario donde aplica > "private ClassFormControlResponsive _FormControlResponsiveData;"</summary>
    public class ClassContainerResponsive
    {
        public List<ClassContainerDataLocalization> ControlsCompData { get; set; }

    }

    /// <summary>Clase para establecer las datos necesarios para las componentes de distribución de los controles en el formulario con control contenedor</summary>
    public class ClassContainerDataLocalization
    {
        public int InitFormHeight { get; set; }
        public int InitFormWidth { get; set; }
        public Control Control { get; set; }
        public int InitX { get; set; }
        public int InitY { get; set; }
        public int InitHeight { get; set; }
        public int InitWidth { get; set; }
        public DockStyle InitialDock { get; set; }
        public AnchorStyles InitialAnchor { get; set; }
        public int InitialLeft { get; set; }
        public int InitialTop{ get; set; }


        public List<ClassContainerDataLocalization> ChildsContainerData { get; set; }

        /// <summary>Define si el control es un contender o no y si este tiene algún elemento incluido</summary>
        public bool HasChilds
        {
            get
            {
                return ChildsContainerData != null && ChildsContainerData.Count > 0;
            }
        }
    }

    public class ClassFormsControlsDataLocalizationMetaProcess
    {
        public List<ClassContainerDataLocalization> CDs { get; set; }

        public int MaxH { get; set; }
        public int TotalW { get; set; }
        public xQuadrant GoTo { get; set; }
        public float YPorcentual { get; set; }
        public int YMean { get; set; }
    }




}
