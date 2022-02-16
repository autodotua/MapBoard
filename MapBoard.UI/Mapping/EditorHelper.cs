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
using System.Windows;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;

namespace MapBoard.Mapping
{
    public class EditorHelper : INotifyPropertyChanged
    {
        /// <summary>
        /// 正在编辑的要素
        /// </summary>
        private Feature editingFeature;

        /// <summary>
        /// 绘制完成的图形
        /// </summary>
        private Geometry geometry = null;

        /// <summary>
        /// 是否正在寻找最近的点
        /// </summary>
        private bool isSearchingNearestPoint = false;

        /// <summary>
        /// 最近的任意点
        /// </summary>
        private MapPoint nearestPoint;

        /// <summary>
        /// 最近结点
        /// </summary>
        private MapPoint nearestVertex;

        public EditorHelper(MainMapView mapView)
        {
            MapView = mapView;
            SketchEditor.GeometryChanged += (s, e) => GeometryChanged?.Invoke(s, e);
            MapView.PreviewMouseRightButtonDown += MapView_PreviewMouseRightButtonDown;
            mapView.PreviewMouseMove += MapView_PreviewMouseMove;
            mapView.GeoViewTapped += MapviewTapped;
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
        public FeatureAttributeCollection Attributes { get; set; }

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
            editingFeature = feature;
            await SketchEditor.StartAsync(feature.Geometry.SpatialReference != MapView.Map.SpatialReference ?
                GeometryEngine.Project(feature.Geometry, MapView.Map.SpatialReference) : feature.Geometry);
            editingFeature = null;
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
            geometry = SketchEditor.Geometry?.EnsureValid();
            Stop();
        }

        /// <summary>
        /// 选择最近的结点
        /// </summary>
        /// <param name="position"></param>
        private void AddPointToSketchEditor(MapPoint point)
        {
            if (point != null)
            {
                if (SketchEditor.Geometry == null)
                {
                    switch (SketchEditor.CreationMode)
                    {
                        case SketchCreationMode.Point:
                            SketchEditor.ReplaceGeometry(point);
                            break;

                        case SketchCreationMode.Multipoint:
                            SketchEditor.ReplaceGeometry(new Multipoint(new[] { point }));
                            break;

                        case SketchCreationMode.Polyline:
                            SketchEditor.ReplaceGeometry(new Polyline(new[] { point }));
                            break;

                        case SketchCreationMode.Polygon:
                            SketchEditor.ReplaceGeometry(new Polygon(new[] { point }));
                            break;
                    }
                    return;
                }
                SketchEditor.InsertVertexAfterSelectedVertex(GeometryEngine.Project(point, SpatialReferences.WebMercator) as MapPoint);
            }
        }

        private bool CanCatchNearestPoint()
        {
            return SketchEditor.CreationMode
                       is not (SketchCreationMode.Arrow
                       or SketchCreationMode.Circle
                       or SketchCreationMode.Ellipse
                       or SketchCreationMode.Rectangle
                       or SketchCreationMode.Triangle
                       or SketchCreationMode.FreehandLine
                       or SketchCreationMode.FreehandPolygon);
        }

        /// <summary>
        /// 获取离指定位置最近的结点
        /// </summary>
        /// <param name="position"></param>
        /// <param name="excludeSelf">是否排除当前正在编辑的图层</param>
        /// <returns></returns>
        private async Task<(MapPoint, MapPoint)> GetNearestVertexAndPointAsync(Point position, bool excludeSelf)
        {
            var location = MapView.ScreenToLocation(position).ToWgs84();
            IReadOnlyList<IdentifyLayerResult> results = null;
            try
            {
                results = await MapView.IdentifyLayersAsync(position, Config.Instance.CatchDistance, false, int.MaxValue);
            }
            catch (Exception ex)
            {
                App.Log.Error("识别最近的图形失败", ex);
                return (null, null);
            }
            MapPoint minVertex = null;
            MapPoint minPoint = null;
            double minVertexDistance = double.MaxValue;
            double minPointDistance = double.MaxValue;
            foreach (var geometry in results
                .Where(p => p.LayerContent is FeatureLayer)
                .SelectMany(p => p.GeoElements)
                .Where(p => !excludeSelf || editingFeature == null || (p as Feature).GetID() != editingFeature.GetID())
                .Select(p => p.Geometry)
                .Select(p => p is Polygon ? (p as Polygon).ToPolyline() : p))
            {
                var nearestVertexResult = GeometryEngine.NearestVertex(geometry, location);
                if (nearestVertexResult != null && nearestVertexResult.Distance < minVertexDistance)
                {
                    minVertex = nearestVertexResult.Coordinate;
                    minVertexDistance = nearestVertexResult.Distance;
                }
                var nearestPointResult = GeometryEngine.NearestCoordinate(geometry, location);
                if (nearestPointResult != null && nearestPointResult.Distance < minPointDistance)
                {
                    minPoint = nearestPointResult.Coordinate;
                    minPointDistance = nearestPointResult.Distance;
                }
            }
            if (minVertex != null)
            {
                //最近任意点一定在要求的范围内，但是最近节点可能大于甚至远大于范围
                var vertexPoint = MapView.LocationToScreen(minVertex);
                var distance = Math.Sqrt((Math.Pow(vertexPoint.X - position.X, 2) + Math.Pow(vertexPoint.Y - position.Y, 2)));
                if (distance > Config.Instance.CatchDistance)
                {
                    return (null, minPoint);
                }
            }
            return (minVertex, minPoint);
        }

        /// <summary>
        /// 鼠标位置移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MapView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!CanCatchNearestPoint()
                || e.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }
            var position = e.GetPosition(MapView);
            if (!isSearchingNearestPoint)
            {
                isSearchingNearestPoint = true;

                (nearestVertex, nearestPoint) = await GetNearestVertexAndPointAsync(position, Config.Instance.AutoCatchToNearestVertex);
                isSearchingNearestPoint = false;
                //因为查询需要一定时间，结束以后可能就不在绘制状态了，所以需要再次判断
                if (MapView.CurrentTask != BoardTask.Draw || !SketchEditor.IsEnabled || !SketchEditor.IsVisible)
                {
                    return;
                }
                if (Config.Instance.ShowNearestPointSymbol || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    MapView.Overlay.SetNearestVertexPoint(nearestVertex);
                }
                else
                {
                    MapView.Overlay.SetNearestVertexPoint(null);
                }
                if (Config.Instance.ShowNearestPointSymbol || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    MapView.Overlay.SetNearestPointPoint(nearestPoint);
                }
                else
                {
                    MapView.Overlay.SetNearestPointPoint(null);
                }
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
            if (!CanCatchNearestPoint())
            {
                return;
            }

            var location = MapView.ScreenToLocation(e.GetPosition(MapView)).ToWgs84();
            ContextMenu menu = new ContextMenu();
            menu.Items.Add(new MenuItem()
            {
                IsEnabled = false,
                Header = new TextBlock()
                {
                    Text = $"经度={location.X:0.000000}{Environment.NewLine}纬度={location.Y:0.000000}",
                    FontSize = 14,
                    Foreground = App.Current.FindResource("SystemControlForegroundBaseHighBrush") as System.Windows.Media.Brush,
                    TextAlignment = TextAlignment.Left,
                }
            });
            if (nearestVertex != null)
            {
                MenuItem item = new MenuItem() { Header = "捕捉最近的结点" + (Config.Instance.ShowNearestPointSymbol ? "（红色）" : "") };
                item.Click += (s, e2) => AddPointToSketchEditor(nearestVertex);
                menu.Items.Add(item);
            }
            if (nearestPoint != null)
            {
                MenuItem item = new MenuItem() { Header = "捕捉最近的结点" + (Config.Instance.ShowNearestPointSymbol ? "（黄色）" : "") };
                item.Click += (s, e2) => AddPointToSketchEditor(nearestPoint);
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private void MapviewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Draw)
            {
                return;
            }
            if (MapView.CurrentTask != BoardTask.Draw || !SketchEditor.IsEnabled || !SketchEditor.IsVisible)
            {
                return;
            }
            if (!CanCatchNearestPoint())
            {
                return;
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (nearestVertex != null)
                {
                    AddPointToSketchEditor(nearestPoint);
                    e.Handled = true;
                }
                return;
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ^ Config.Instance.AutoCatchToNearestVertex)//只有一个可以为true
            {
                if (nearestVertex != null)
                {
                    AddPointToSketchEditor(nearestVertex);
                    e.Handled = true;
                }
                return;
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
            MapView.Overlay.SetNearestVertexPoint(null);
            MapView.Overlay.SetNearestPointPoint(null);
        }
    }
}