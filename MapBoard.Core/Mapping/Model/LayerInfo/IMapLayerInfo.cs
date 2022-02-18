using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Model;
using System;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public interface IMapLayerInfo : ILayerInfo, IDisposable
    {
        /// <summary>
        /// 图层的图形类型
        /// </summary>
        GeometryType GeometryType { get; }

        /// <summary>
        /// 图层关联的Esri矢量图层
        /// </summary>
        FeatureLayer Layer { get; }

        /// <summary>
        /// 图层包含的要素数量
        /// </summary>
        long NumberOfFeatures { get; }

        /// <summary>
        /// 查询图层边界范围
        /// </summary>
        Task<Envelope> QueryExtentAsync(QueryParameters parameters);

        /// <summary>
        /// 查询图层的要素
        /// </summary>
        Task<FeatureQueryResult> QueryFeaturesAsync(QueryParameters parameters);

        /// <summary>
        /// 是否启用了时间限制
        /// </summary>
        //bool TimeExtentEnable { get; set; }

        /// <summary>
        /// 图层和地图界面解绑事件
        /// </summary>
        event EventHandler Unattached;

        /// <summary>
        /// 修改图层名
        /// </summary>
        /// <param name="newName"></param>
        /// <param name="layers"></param>
        /// <returns></returns>
        Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers);

        /// <summary>
        /// 获取图层默认符号
        /// </summary>
        /// <returns></returns>
        SymbolInfo GetDefaultSymbol();

        /// <summary>
        /// 加载图层
        /// </summary>
        /// <returns></returns>
        Task LoadAsync();

        /// <summary>
        /// 重新加载图层，并刷新Esri图层
        /// </summary>
        /// <param name="layers"></param>
        /// <returns></returns>
        Task ReloadAsync(MapLayerCollection layers);

        /// <summary>
        /// 获取添加到Esri图层集合中的图层
        /// </summary>
        /// <returns></returns>
        Layer GetAddedLayer();

        /// <summary>
        /// 是否已经加载
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// 加载失败信息
        /// </summary>
        Exception LoadError { get; }
        bool CanEdit { get; }
    }
}