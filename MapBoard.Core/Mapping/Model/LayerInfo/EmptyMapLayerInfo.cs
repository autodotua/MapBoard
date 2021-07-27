using Esri.ArcGISRuntime.Data;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public class EmptyMapLayerInfo : MapLayerInfo
    {
        public override string Type => "Empty";

        protected override FeatureTable GetTable()
        {
            return null;
        }

        private EmptyMapLayerInfo()
        {
        }

        public static MapLayerInfo CreateTemplate()
        {
            return new EmptyMapLayerInfo();
        }

        public async override Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
        }
    }
}