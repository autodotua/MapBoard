using ModernWpf.FzExtension.CommonDialog;
using MapBoard.Mapping.Model;

namespace MapBoard.UI.Dialog
{
    public abstract class AddLayerDialogBase : CommonDialog
    {
        public AddLayerDialogBase(MapLayerCollection layers)
        {
            Layers = layers;
        }
        public AddLayerDialogBase()
        {
        }
        /// <summary>
        /// 图层名
        /// </summary>
        public string LayerName { get; set; }

        /// <summary>
        /// 图层集合
        /// </summary>
        public MapLayerCollection Layers { get; }

        /// <summary>
        /// 向用户展示的信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 服务链接
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 是否自动下载全部
        /// </summary>
        public bool AutoPopulateAll { get; set; }
    }
}