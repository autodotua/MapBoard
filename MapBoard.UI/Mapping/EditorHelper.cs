using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MapBoard.Mapping.Model;
using System.Windows.Controls;
using MapBoard.Util;
using Esri.ArcGISRuntime.Symbology;

namespace MapBoard.Mapping
{
    public class EditorHelper : INotifyPropertyChanged
    {
        /// <summary>
        /// 正在编辑的要素的属性
        /// </summary>
        private FeatureAttributeCollection attributes;

        /// <summary>
        /// 绘制完成的图形
        /// </summary>
        private Geometry geometry = null;

        /// <summary>
        /// 是否正在寻找最近的点
        /// </summary>
        private bool isSearchingNearestPoint = false;

        /// <summary>
        /// 最近结点
        /// </summary>
        private MapPoint nearestPoint;

        public EditorHelper(MainMapView mapView)
        {
            MapView = mapView;
            SketchEditor.GeometryChanged += (s, e) => GeometryChanged?.Invoke(s, e);
            MapView.PreviewMouseRightButtonDown += MapView_PreviewMouseRightButtonDown;
            mapView.PreviewMouseMove += MapView_PreviewMouseMove;
        }

        /// <summary>
        /// 编辑器状态发生改变
        /// </summary>
        public event EventHandler<EditorStatusChangedEventArgs> EditorStatusChanged;

        /// <summary>
        /// 编辑时图形发生改变
        /// </summary>
        public event EventHandler<GeometryChangedEventArgs> GeometryChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 正在编辑的要素的属性
        /// </summary>
        public FeatureAttributeCollection Attributes
        {
            get => attributes;
            set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

        public MapLayerCollection Layers => MapView.Layers;

        public MainMapView MapView { get; }

        /// <summary>
        /// 当前正在进行的绘制操作的类型
        /// </summary>
        public EditMode Mode { get; private set; }

        public SketchEditor SketchEditor => MapView.SketchEditor;

        /// <summary>
        /// 停止并不保存
        /// </summary>
        public void Cancel()
        {
            if (Mode == EditMode.None)
            {
                return;
            }
            geometry = null;
            Stop();
        }

        /// <summary>
        /// 绘制新的图形
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task DrawAsync(SketchCreationMode mode)
        {
            if (Layers.Selected is not IEditableLayerInfo)
            {
                throw new NotSupportedException("选中的图层不支持编辑");
            }
            var layer = Layers.Selected as IEditableLayerInfo;
            if (Attributes != null)
            {
                var label = Attributes.Label;
                var key = Attributes.Key;
                var date = Attributes.Date;
                Attributes = FeatureAttributeCollection.Empty(Layers.Selected);
                if (Config.Instance.RemainLabel)
                {
                    Attributes.Label = label;
                }
                if (Config.Instance.RemainKey)
                {
                    Attributes.Key = key;
                }
                if (Config.Instance.RemainDate)
                {
                    Attributes.Date = date;
                }
            }
            else
            {
                Attributes = FeatureAttributeCollection.Empty(Layers.Selected);
            }
            StartDraw(EditMode.Creat);

            await SketchEditor.StartAsync(mode);
            if (geometry != null)
            {
                Feature feature = layer.CreateFeature();
                feature.Geometry = geometry;
                Attributes.SaveToFeature(feature);
                await layer.AddFeatureAsync(feature, FeaturesChangedSource.Draw);
            }
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <returns></returns>
        public async Task EditAsync(IEditableLayerInfo layer, Feature feature)
        {
            Attributes = FeatureAttributeCollection.FromFeature(layer, feature);
            StartDraw(EditMode.Edit);
            await SketchEditor.StartAsync(feature.Geometry.SpatialReference != MapView.Map.SpatialReference ?
                GeometryEngine.Project(feature.Geometry, MapView.Map.SpatialReference) : feature.Geometry);
            if (geometry != null)
            {
                UpdatedFeature newFeature = new UpdatedFeature(feature, feature.Geometry, new Dictionary<string, object>(feature.Attributes));
                feature.Geometry = geometry;
                Attributes.SaveToFeature(feature);
                await layer.UpdateFeatureAsync(newFeature, FeaturesChangedSource.Edit);
            }
        }

        /// 获取一个多点
        /// </summary>
        /// <returns></returns>
        public async Task<Multipoint> GetMultiPointAsync()
        {
            StartDraw(EditMode.GetGeometry);
            var geom = await SketchEditor.StartAsync(SketchCreationMode.Multipoint, false);
            Cancel();
            if (geom is Multipoint point)
            {
                return point;
            }
            return null;
        }

        /// <summary>
        /// 获取一个点
        /// </summary>
        /// <returns></returns>
        public async Task<MapPoint> GetPointAsync()
        {
            StartDraw(EditMode.GetGeometry);
            var geom = await SketchEditor.StartAsync(SketchCreationMode.Point, false);
            Cancel();
            if (geom is MapPoint point)
            {
                return point;
            }
            return null;
        }

        /// <summary>
        /// 获取一个多边形
        /// </summary>
        /// <returns></returns>
        public async Task<Polygon> GetPolygonAsync()
        {
            StartDraw(EditMode.GetGeometry);
            var geometry = await SketchEditor.StartAsync(SketchCreationMode.Polygon, false);
            StopAndSave();
            if (geometry is Polygon rect)
            {
                return rect;
            }
            return null;
        }

        /// <summary>
        /// 获取一条折线
        /// </summary>
        /// <returns></returns>
        public async Task<Polyline> GetPolylineAsync()
        {
            StartDraw(EditMode.GetGeometry);
            await SketchEditor.StartAsync(SketchCreationMode.Polyline);
            if (geometry is Polyline line)
            {
                if (line.Parts[0].PointCount > 1)
                {
                    return line;
                }
                return null;
            }
            return null;
        }

        /// <summary>
        /// 获取一个矩形
        /// </summary>
        /// <returns></returns>
        public async Task<Envelope> GetRectangleAsync()
        {
            StartDraw(EditMode.GetGeometry);
            var geometry = await SketchEditor.StartAsync(SketchCreationMode.Rectangle, false);
            StopAndSave();
            if (geometry is Polygon rect)
            {
                return rect.Extent;
            }
            return null;
        }

        /// <summary>
        /// 测量面积
        /// </summary>
        /// <returns></returns>
        public async Task MeasureArea()
        {
            StartDraw(EditMode.MeasureArea);
            await SketchEditor.StartAsync(SketchCreationMode.Polygon);
        }

        /// <summary>
        /// 测量长度
        /// </summary>
        /// <returns></returns>
        public async Task MeasureLength()
        {
            StartDraw(EditMode.MeasureLength);
            await SketchEditor.StartAsync(SketchCreationMode.Polyline);
        }

        /// <summary>
        /// 停止并保存当前结果
        /// </summary>
        public void StopAndSave()
        {
            if (Mode == EditMode.None)
            {
                return;
            }
            geometry = SketchEditor.Geometry;
            Stop();
        }

        /// <summary>
        /// 获取离指定位置最近的结点
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private async Task<MapPoint> GetNearestPointAsync(System.Windows.Point position)
        {
            var location = GeometryEngine.Project(MapView.ScreenToLocation(position), SpatialReferences.Wgs84) as MapPoint;
            var results = await MapView.IdentifyLayersAsync(position, 20, false, int.MaxValue);
            MapPoint nearestPoint = null;
            double minDistance = double.MaxValue;
            foreach (var result in results)
            {
                foreach (var geometry in result.GeoElements.Select(p => p.Geometry))
                {
                    foreach (var point in geometry.GetPoints())
                    {
                        var distance = GeometryUtility.GetDistance(location, point);
                        nearestPoint = nearestPoint == null ? point :
                           (distance < minDistance ? point : nearestPoint);
                        minDistance = Math.Min(minDistance, distance);
                    }
                }
            }
            return nearestPoint;
        }

        /// <summary>
        /// 鼠标位置移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MapView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Draw || !SketchEditor.IsEnabled || !SketchEditor.IsVisible)
            {
                return;
            }
            if (SketchEditor.Geometry == null)
            {
                switch (SketchEditor.CreationMode)
                {
                    case SketchCreationMode.Arrow:
                    case SketchCreationMode.Circle:
                    case SketchCreationMode.Ellipse:
                    case SketchCreationMode.Rectangle:
                    case SketchCreationMode.Triangle:
                    case SketchCreationMode.FreehandLine:
                    case SketchCreationMode.FreehandPolygon:
                        return;
                }
            }
            if (!isSearchingNearestPoint)
            {
                isSearchingNearestPoint = true;
                nearestPoint = await GetNearestPointAsync(e.GetPosition(MapView));
                MapView.Overlay.SetDrawPoint(nearestPoint);
                isSearchingNearestPoint = false;
            }
        }

        /// <summary>
        /// 鼠标右键单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Draw || !SketchEditor.IsEnabled || !SketchEditor.IsVisible)
            {
                return;
            }
            if (SketchEditor.Geometry == null)
            {
                switch (SketchEditor.CreationMode)
                {
                    case SketchCreationMode.Arrow:
                    case SketchCreationMode.Circle:
                    case SketchCreationMode.Ellipse:
                    case SketchCreationMode.Rectangle:
                    case SketchCreationMode.Triangle:
                    case SketchCreationMode.FreehandLine:
                    case SketchCreationMode.FreehandPolygon:
                        return;
                }
            }
            if (nearestPoint != null)
            {
                ContextMenu menu = new ContextMenu();
                MenuItem item = new MenuItem() { Header = "选择最近的节点" };
                item.Click += (s, e2) => SelectNearestPoint();
                menu.Items.Add(item);
                menu.IsOpen = true;
            }
        }

        /// <summary>
        /// 选择最近的结点
        /// </summary>
        /// <param name="position"></param>
        private void SelectNearestPoint()
        {
            if (nearestPoint != null)
            {
                if (SketchEditor.Geometry == null)
                {
                    switch (SketchEditor.CreationMode)
                    {
                        case SketchCreationMode.Point:
                            SketchEditor.ReplaceGeometry(nearestPoint);
                            break;

                        case SketchCreationMode.Multipoint:
                            SketchEditor.ReplaceGeometry(new Multipoint(new[] { nearestPoint }));
                            break;

                        case SketchCreationMode.Polyline:
                            SketchEditor.ReplaceGeometry(new Polyline(new[] { nearestPoint }));
                            break;

                        case SketchCreationMode.Polygon:
                            SketchEditor.ReplaceGeometry(new Polygon(new[] { nearestPoint }));
                            break;
                    }
                    return;
                }
                SketchEditor.InsertVertexAfterSelectedVertex(GeometryEngine.Project(nearestPoint, SpatialReferences.WebMercator) as MapPoint);
            }
        }

        /// <summary>
        /// 开始绘制
        /// </summary>
        /// <param name="mode"></param>
        private void StartDraw(EditMode mode)
        {
            Mode = mode;
            MapView.CurrentTask = BoardTask.Draw;
            EditorStatusChanged?.Invoke(this, new EditorStatusChangedEventArgs(true));
        }

        /// <summary>
        /// 停止绘制
        /// </summary>
        private void Stop()
        {
            Mode = EditMode.None;
            SketchEditor.Stop();
            EditorStatusChanged?.Invoke(this, new EditorStatusChangedEventArgs(false));
            MapView.CurrentTask = BoardTask.Ready;
            MapView.Overlay.SetDrawPoint(null);
        }
    }
}