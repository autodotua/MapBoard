using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        public LayerDialogBase(Window owner, IMapLayerInfo layer, MainMapView mapView) : base(owner)
        {
            ShowInTaskbar = false;
            Layer = layer;
            MapView = mapView;
            //图层被移除或删除后，需要关闭本窗口
            layer.Unattached += (p1, p2) =>
            {
                closing = true;
                Close();
            };
        }

        public IMapLayerInfo Layer { get; }
        protected MainMapView MapView { get; }

        protected bool closing = false;
        private static Dictionary<IMapLayerInfo, LayerDialogBase> dialogs = new Dictionary<IMapLayerInfo, LayerDialogBase>();

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Debug.Assert(dialogs.ContainsKey(Layer));
            dialogs.Remove(Layer);
        }

        protected static T GetInstance<T>(IMapLayerInfo layer, Func<T> getInstance) where T : LayerDialogBase
        {
            if (dialogs.ContainsKey(layer))
            {
                return dialogs[layer] as T;
            }
            var dialog = getInstance();
            dialogs.Add(layer, dialog);
            return dialog;
        }
    }
}