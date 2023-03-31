using System;

namespace MapBoard.Model
{
    /// <summary>
    /// 底图类型
    /// </summary>
    public enum BaseLayerType
    {
        /// <summary>
        /// XYZ网络切片
        /// </summary>
        WebTiledLayer,

        /// <summary>
        /// 基于文件的矢量图
        /// </summary>
        RasterLayer,

        /// <summary>
        /// 基于文件的Shapefile
        /// </summary>
        ShapefileLayer,

        /// <summary>
        /// 瓦片图层包
        /// </summary>
        TpkLayer,

        /// <summary>
        /// WMS图层
        /// </summary>
        WmsLayer,

        /// <summary>
        /// ESRI自带图层（启用）
        /// </summary>
        [Obsolete]
        Esri,

        /// <summary>
        /// WMTS图层
        /// </summary>
        WmtsLayer
    }
}