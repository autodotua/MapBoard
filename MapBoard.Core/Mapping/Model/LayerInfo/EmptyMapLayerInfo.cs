using Esri.ArcGISRuntime.Data;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 用户不可见的空图层，用于在进行几何处理时，临时使用
    /// </summary>
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

        public override Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            return Task.CompletedTask;
        }

        public override Task DeleteAsync()
        {
            return Task.CompletedTask;
        }
    }
}