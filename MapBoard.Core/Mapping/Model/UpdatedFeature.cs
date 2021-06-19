using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using System.Collections.Generic;

namespace MapBoard.Mapping.Model
{
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

        public Feature Feature { get; }
        public Geometry OldGeometry { get; }
        public IDictionary<string, object> OldAttributes { get; }
    }
}