using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.Common
{
    /// <summary>Añadir al formulario donde aplica > "private ClassFormControlResponsive _FormControlResponsiveData;"</summary>
    public class ClassFormControlResponsive
    {
        public int ActualFormH { get; set; }
        public int ActualFormW { get; set; }

        public List<ClassFormControlsDataLocalization> ControlsCompData { get; set; }

    }

    /// <summary>Clase para establecer las datos necesarios para las componentes de distribución de los controles en el formulario con control contenedor</summary>
    public class ClassFormControlsDataLocalization
    {        
        public Control Control { get; set; }
        public int InitLocX { get; set; }
        public int InitLocY { get; set; }
        public int InitControlH { get; set; }
        public int InitControlW { get; set; }

        public List<ClassFormControlsDataLocalization> ChildsControlsData { get; set; }

        /// <summary>Define si el control es un contender o no y si este tiene algún elemento incluido</summary>
        public Boolean HasChildControls
        {
            get
            {
                return ChildsControlsData != null && ChildsControlsData.Count > 0;
            } 
        }
    }

    public class ClassFormsControlsDataLocalizationMetaProcess
    {
        public List<ClassFormControlsDataLocalization> CDs { get; set; }

        public Int32 MaxH { get; set; }
        public Int32 TotalW { get; set; }
        public xQuadrant GoTo { get; set; }        
        public float YPorcentual { get; set; }
        public Int32 YMean { get; set; }

    }

    public enum xQuadrant
    {
        Center,
        Left,
        Right
    }



}
