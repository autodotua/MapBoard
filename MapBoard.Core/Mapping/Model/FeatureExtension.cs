using Esri.ArcGISRuntime.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public static class FeatureExtension
    {
        public static long GetFID(this Feature feature)
        {
            if(feature.Attributes.ContainsKey("FID"))
            {
                return (long)feature.GetAttributeValue("FID");
            }
            if(feature.Attributes.ContainsKey("ID"))
            {
                return (long)feature.GetAttributeValue("ID");
            }
            if(feature.Attributes.ContainsKey("ObjectID"))
            {
                return (long)feature.GetAttributeValue("ObjectID");
            }
            return feature.Geometry.ToJson().GetHashCode();
        }
    }
}