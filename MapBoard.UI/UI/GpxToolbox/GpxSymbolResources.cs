using Esri.ArcGISRuntime.Symbology;
using System.Drawing;

namespace MapBoard.UI.GpxToolbox
{
    /// <summary>
    /// GPX工具箱的符号资源
    /// </summary>
    public static class GpxSymbolResources
    {
        /// <summary>
        /// 游览中的点渲染器
        /// </summary>
        public static SimpleRenderer BrowsePointRenderer =>
            new SimpleRenderer(BrowsePointSymbol);

        /// <summary>
        /// 游览点符号
        /// </summary>
        public static SimpleMarkerSymbol BrowsePointSymbol { get; } = new SimpleMarkerSymbol()
        {
            Color = Color.Blue,
            Size = 12,
            Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.White, 2),
            Style = SimpleMarkerSymbolStyle.Circle,
        };


        /// <summary>
        /// 未选择的点符号
        /// </summary>
        public static SimpleMarkerSymbol NotSelectedPointSymbol { get; } = new SimpleMarkerSymbol()
        {
            Color = Color.Blue,
            Size = 3,
            Style = SimpleMarkerSymbolStyle.Circle,
        };

        /// <summary>
        /// 选择的点符号
        /// </summary>
        public static SimpleMarkerSymbol SelectedPointSymbol { get; } = new SimpleMarkerSymbol()
        {
            Color = Color.Red,
            Size = 6,
            Style = SimpleMarkerSymbolStyle.Circle,
        };

        /// <summary>
        /// 选中的点渲染器
        /// </summary>
        public static SimpleRenderer SelectionRenderer =>
            new SimpleRenderer(NotSelectedPointSymbol);

    }
}