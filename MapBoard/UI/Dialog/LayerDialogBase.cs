using MapBoard.Common;
using MapBoard.Main.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapBoard.Main.UI.Dialog
{
    public class LayerDialogBase : DialogWindowBase
    {
        public LayerDialogBase(Window owner, MapLayerInfo layer) : base(owner)
        {
            Layer = layer;
            layer.Unattached += (p1, p2) =>
            {
                closing = true;
                Close();
            };
        }

        public MapLayerInfo Layer { get; }
        protected bool closing = false;
    }
}