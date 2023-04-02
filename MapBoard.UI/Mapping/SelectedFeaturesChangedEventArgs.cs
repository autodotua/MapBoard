using Esri.ArcGISRuntime.Data;
using MapBoard.Mapping.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 选择的要素发生改变事件
    /// </summary>
    public class SelectedFeaturesChangedEventArgs : EventArgs
    {
        public SelectedFeaturesChangedEventArgs(IMapLayerInfo layer, IEnumerable<Feature> selected, IEnumerable<Feature> unSelected)
        {
            if (selected != null)
            {
                Selected = selected.ToArray();
            }
            else
            {
                Selected = Array.Empty<Feature>();
            }
            if (unSelected != null)
            {
                UnSelected = unSelected.ToArray();
            }
            else
            {
                UnSelected = Array.Empty<Feature>();
            }
            Layer = layer;
        }

        /// <summary>
        /// 新选择的要素
        /// </summary>
        public Feature[] Selected { get; }

        /// <summary>
        /// 排除选择的要素
        /// </summary>
        public Feature[] UnSelected { get; }

        /// <summary>
        /// 要素所属图层
        /// </summary>
        public IMapLayerInfo Layer { get; }
    }
}