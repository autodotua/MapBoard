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
        /// <summary>
        /// 是否正在关闭
        /// </summary>
        protected bool closing = false;

        /// <summary>
        /// 从图层到对话框的映射
        /// </summary>
        private static Dictionary<IMapLayerInfo, LayerDialogBase> dialogs = new Dictionary<IMapLayerInfo, LayerDialogBase>();

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

        /// <summary>
        /// 图层
        /// </summary>
        public IMapLayerInfo Layer { get; }

        /// <summary>
        /// 地图
        /// </summary>
        protected MainMapView MapView { get; }
        /// <summary>
        /// 获取指定图层的对应实例。若不存在，则创建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="layer"></param>
        /// <param name="getInstance"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 对话框关闭
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Debug.Assert(dialogs.ContainsKey(Layer));
            dialogs.Remove(Layer);
        }
    }
}