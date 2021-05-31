using Esri.ArcGISRuntime.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.UI.Map.Model
{
    public static class FeatureExtension
    {
        public static long GetFID(this Feature feature)
        {
            return (long)feature.GetAttributeValue("FID");
        }
    }
}