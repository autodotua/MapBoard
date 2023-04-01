using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using MapBoard.Mapping.Model;
using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 3D浏览模式地图
    /// </summary>
    [DoNotNotify]
    public class BrowseSceneView : SceneView, INotifyPropertyChanged, IMapBoardGeoView
    {
        private static List<BrowseSceneView> instances = new List<BrowseSceneView>();

        public BrowseSceneView()
        {
            instances.Add(this);
            IsAttributionTextVisible = false;
            AllowDrop = false;
            this.SetHideWatermark();
        }

        /// <summary>
        /// 图层
        /// </summary>
        public MapLayerCollection Layers { get; private set; }

        /// <summary>
        /// 覆盖层
        /// </summary>
        public OverlayHelper Overlay { get; private set; }

        /// <summary>
        /// 请求绘制一个点
        /// </summary>
        /// <returns></returns>
        public Task<MapPoint> GetPointAsync()
        {
            return SceneEditHelper.CreatePointAsync(this);
        }

        /// <summary>
        /// 加载地图
        /// </summary>
        /// <returns></returns>
        public async Task LoadAsync()
        {
            await GeoViewHelper.LoadBaseGeoViewAsync(this, Config.Instance.EnableBasemapCache);
            Layers = await MapLayerCollection.GetInstanceAsync(Scene.OperationalLayers);
            ZoomToLastExtent().ConfigureAwait(false);
            Overlay = new OverlayHelper(GraphicsOverlays, async p => await ZoomToGeometryAsync(p));
        }

        /// <summary>
        /// 缩放到指定范围
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="autoExtent"></param>
        /// <returns></returns>
        public Task ZoomToGeometryAsync(Geometry geometry, bool autoExtent = true)
        {
            if (geometry is MapPoint)
            {
                if (geometry.SpatialReference.Wkid != SpatialReferences.WebMercator.Wkid)
                {
                    geometry = GeometryEngine.Project(geometry, SpatialReferences.WebMercator);
                }
                geometry = GeometryEngine.Buffer(geometry, 100);
            }
            return SetViewpointAsync(new Viewpoint(geometry));
        }

        /// <summary>
        /// 缩放到记忆的地图范围
        /// </summary>
        /// <returns></returns>
        private async Task ZoomToLastExtent()
        {
            if (Layers.MapViewExtentJson != null)
            {
                try
                {
                    await ZoomToGeometryAsync(Envelope.FromJson(Layers.MapViewExtentJson), false);
                }
                catch
                {
                }
            }
        }
    }
}