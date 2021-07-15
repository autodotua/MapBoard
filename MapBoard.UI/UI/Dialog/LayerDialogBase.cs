using MapBoard.Mapping.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 指定图层的非模态窗口对话框的基类
    /// </summary>
    public class LayerDialogBase : DialogWindowBase
    {
        public LayerDialogBase(Window owner, IMapLayerInfo layer) : base(owner)
        {
            Layer = layer;
            //图层被移除或删除后，需要关闭本窗口
            layer.Unattached += (p1, p2) =>
            {
                closing = true;
                Close();
            };
        }

        public IMapLayerInfo Layer { get; }
        protected bool closing = false;
    }
}