using Esri.ArcGISRuntime.Data;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 用户不可见的空图层，用于在进行几何处理时，临时使用
    /// </summary>
    public class EmptyMapLayerInfo : MapLayerInfo
    {
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

        public override Task DeleteAsync()
        {
            return Task.CompletedTask;
        }
    }
}