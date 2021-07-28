using Esri.ArcGISRuntime.Geometry;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 表示能够改变几何类型的图层
    /// </summary>
    public interface ICanChangeGeometryType
    {
        void SetGeometryType(GeometryType type);
    }
}