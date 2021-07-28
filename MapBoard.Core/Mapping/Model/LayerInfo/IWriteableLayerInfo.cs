using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 表示可编辑的图层
    /// </summary>
    public interface IEditableLayerInfo : IMapLayerInfo
    {
        /// <summary>
        /// 要素操作历史
        /// </summary>
        ObservableCollection<FeaturesChangedEventArgs> Histories { get; }

        /// <summary>
        /// 要素发生增删改
        /// </summary>
        event EventHandler<FeaturesChangedEventArgs> FeaturesChanged;

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
        Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source);

        /// <summary>
        /// 添加一组要素
        /// </summary>
        /// <param name="features"></param>
        /// <param name="source"></param>
        /// <param name="rebuildFeatures"></param>
        /// <returns></returns>
        Task AddFeaturesAsync(IEnumerable<Feature> features, FeaturesChangedSource source, bool rebuildFeatures);

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