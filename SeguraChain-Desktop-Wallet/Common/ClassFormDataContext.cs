using SeguraChain_Desktop_Wallet.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.Common
{
    public class ClassDataContextForm
    {
        public ClassContainerResponsive FormContainerResponsiveData { get; set; }

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

        private static void recursiveSearchControlsChilds(Form f1, List<ClassContainerDataLocalization> lCD, Control cD_BF)
        {

            foreach (Control c in cD_BF == null ? f1.Controls : cD_BF.Controls)
            {
                ClassContainerDataLocalization cD =
                    new ClassContainerDataLocalization()
                    {
                        InitFormHeight = f1.Height,
                        InitFormWidth = f1.Width,
                        Control = c,
                        InitX = c.Location.X,
                        InitY = c.Location.Y,
                        InitHeight = c.Height,
                        InitWidth = c.Width
                    };

                lCD.Add(cD);

                if (c.Controls != null && c.Controls.Count > 0)
                {
                    cD.ChildsContainerData = new List<ClassContainerDataLocalization>();
                    recursiveSearchControlsChilds(f1, cD.ChildsContainerData, c);
                }
            }
        }
    }
}
