using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public interface IMapLayerInfo : ILayerInfo, IDisposable
    {
        /// <summary>
        /// 要素发生增删改
        /// </summary>
        event EventHandler<FeaturesChangedEventArgs> FeaturesChanged;

        /// <summary>
        /// 图层和地图界面解绑事件
        /// </summary>
        event EventHandler Unattached;

        /// <summary>
        /// 是否允许编辑
        /// </summary>
        bool CanEdit { get; }

        /// <summary>
        /// 图层的图形类型
        /// </summary>
        GeometryType GeometryType { get; }

        /// <summary>
        /// 要素操作历史
        /// </summary>
        ObservableCollection<FeaturesChangedEventArgs> Histories { get; }

        /// <summary>
        /// 是否已经加载
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// 图层关联的Esri矢量图层
        /// </summary>
        FeatureLayer Layer { get; }

        /// <summary>
        /// 加载失败信息
        /// </summary>
        Exception LoadError { get; }

        /// <summary>
        /// 图层包含的要素数量
        /// </summary>
        long NumberOfFeatures { get; }

        /// <summary>
        /// 添加一个要素，并自动判断是否应该重建
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task AddFeatureAsync(Feature feature, FeaturesChangedSource source);

        /// <summary>
        /// 添加一个要素
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <param name="rebuildFeatures"></param>
        /// <returns></returns>
        Task AddFeatureAsync(Feature feature, FeaturesChangedSource source, bool rebuildFeatures);

        /// <summary>
        /// 添加一组要素，并自动判断是否应该重建
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task<IEnumerable<Feature>> AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source);

        /// <summary>
        /// 添加一组要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <param name="rebuildFeatures"></param>
        /// <returns></returns>
        Task<IEnumerable<Feature>> AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source, bool rebuildFeatures);

        /// <summary>
        /// 创建空要素
        /// </summary>
        /// <returns></returns>
        Feature CreateFeature();

        /// <summary>
        /// 根据提供的属性和图形创建要素
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        Feature CreateFeature(IEnumerable<KeyValuePair<string, object>> attributes, Geometry geometry);

        /// <summary>
        /// 删除一个要素
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task DeleteFeatureAsync(Feature feature, FeaturesChangedSource source);

        /// <summary>
        /// 删除一组要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task DeleteFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source);

        /// <summary>
        /// 是否启用了时间限制
        /// </summary>
        //bool TimeExtentEnable { get; set; }
        /// <summary>
        /// 获取图层默认符号
        /// </summary>
        /// <returns></returns>
        SymbolInfo GetDefaultSymbol();

        /// <summary>
        /// 获取添加到Esri图层集合中的图层
        /// </summary>
        /// <returns></returns>
        Layer GetLayerForLayerList();

        /// <summary>
        /// 加载图层
        /// </summary>
        /// <returns></returns>
        Task LoadAsync();

        /// <summary>
        /// 查询图层边界范围
        /// </summary>
        Task<Envelope> QueryExtentAsync(QueryParameters parameters);

        /// <summary>
        /// 查询图层的要素
        /// </summary>
        Task<FeatureQueryResult> QueryFeaturesAsync(QueryParameters parameters);
        /// <summary>
        /// 重新加载图层，并刷新Esri图层
        /// </summary>
        /// <param name="layers"></param>
        /// <returns></returns>
        Task ReloadAsync(MapLayerCollection layers);
        /// <summary>
        /// 更新一个要素
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task UpdateFeatureAsync(UpdatedFeature feature, FeaturesChangedSource source);

        /// <summary>
        /// 更新一组要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        Task UpdateFeaturesAsync(IEnumerable<UpdatedFeature> features, FeaturesChangedSource source);
    }
}