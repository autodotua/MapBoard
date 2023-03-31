using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using System.Collections.Generic;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 要素更新信息  
    /// </summary>
    public class UpdatedFeature
    {
        public UpdatedFeature(Feature newFeature)
        {
            Feature = newFeature;
            OldGeometry = Feature.Geometry;
            OldAttributes = new Dictionary<string, object>(Feature.Attributes);
        }

        public UpdatedFeature(Feature newFeature, Geometry oldGeometry, IDictionary<string, object> oldAttributes)
        {
            Feature = newFeature;
            OldGeometry = oldGeometry;
            OldAttributes = oldAttributes;
        }

        /// <summary>
        /// 对应要素
        /// </summary>
        public Feature Feature { get; }

        /// <summary>
        /// 修改前的要素属性
        /// </summary>
        public IDictionary<string, object> OldAttributes { get; }

        /// <summary>
        /// 修改前的要素图形
        /// </summary>
        public Geometry OldGeometry { get; }
    }
}