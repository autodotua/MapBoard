using Esri.ArcGISRuntime.Data;
using MapBoard.Mapping.Model;

namespace MapBoard.Model
{
    /// <summary>
    /// 选择的要素信息
    /// </summary>
    public class FeatureSelectionInfo
    {
        public FeatureSelectionInfo(IMapLayerInfo layer, Feature feature, int index)
        {
            Feature = feature;
            Index = index;
            Attributes = FeatureAttributeCollection.FromFeature(layer, feature);
        }

        /// <summary>
        /// 要素相关属性
        /// </summary>
        public FeatureAttributeCollection Attributes { get; set; }

        /// <summary>
        /// 要素
        /// </summary>
        public Feature Feature { get; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Index { get; }
    }
}