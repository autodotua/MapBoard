using Esri.ArcGISRuntime.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 要素改变事件
    /// </summary>
    public class FeaturesChangedEventArgs : EventArgs, INotifyPropertyChanged
    {
        public FeaturesChangedEventArgs(MapLayerInfo layer,
            IEnumerable<Feature> addedFeatures,
            IEnumerable<Feature> deletedFeatures,
            IEnumerable<UpdatedFeature> changedFeatures,
            FeaturesChangedSource source)
        {
            Source = source;
            Time = DateTime.Now;
            int count = 0;
            if (deletedFeatures != null)
            {
                count++;
                DeletedFeatures = new List<Feature>(deletedFeatures).AsReadOnly();
            }
            if (addedFeatures != null)
            {
                count++;
                AddedFeatures = new List<Feature>(addedFeatures).AsReadOnly();
            }
            if (changedFeatures != null)
            {
                count++;
                UpdatedFeatures = new List<UpdatedFeature>(changedFeatures).AsReadOnly();
            }
            Debug.Assert(count == 1);
            Layer = layer;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 增加的要素集合
        /// </summary>
        public IReadOnlyList<Feature> AddedFeatures { get; }
        /// <summary>
        /// 是否允许撤销操作
        /// </summary>
        public bool CanUndo { get; set; } = true;
        /// <summary>
        /// 删除的要素集合
        /// </summary>
        public IReadOnlyList<Feature> DeletedFeatures { get; }
        /// <summary>
        /// 要素所属图层
        /// </summary>
        public MapLayerInfo Layer { get; }
        /// <summary>
        /// 要素发生改变的操作源
        /// </summary>
        public FeaturesChangedSource Source { get; }
        /// <summary>
        /// 发生改变的时间
        /// </summary>
        public DateTime Time { get; }
        /// <summary>
        /// 更新的要素集合
        /// </summary>
        public IReadOnlyList<UpdatedFeature> UpdatedFeatures { get; }
    }
}