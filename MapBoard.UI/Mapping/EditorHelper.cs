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
        private MapPoint nearestVertex;

        /// <summary>
        /// 最近的任意点
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
        private async Task<(MapPoint, MapPoint)> GetNearestVertexAndPointAsync(System.Windows.Point position)
        {
            var location = GeometryEngine.Project(MapView.ScreenToLocation(position), SpatialReferences.Wgs84) as MapPoint;
            var results = await MapView.IdentifyLayersAsync(position, 20, false, int.MaxValue);
            MapPoint minVertex = null;
            MapPoint minPoint = null;
            double minVertexDistance = double.MaxValue;
            double minPointDistance = double.MaxValue;
            foreach (var result in results)
            {
                foreach (var geometry in result.GeoElements.Select(p => p.Geometry))
                {
                    var nearestVertexResult = GeometryEngine.NearestVertex(geometry, location);
                    if (nearestVertexResult != null && nearestVertexResult.Distance < minVertexDistance)
                    {
                        minVertex = nearestVertexResult.Coordinate;
                    }
                    var nearestPointResult = GeometryEngine.NearestCoordinate(geometry, location);
                    if (nearestPointResult != null && nearestPointResult.Distance < minPointDistance)
                    {
                        minPoint = nearestPointResult.Coordinate;
                    }
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
            if (MapView.CurrentTask != BoardTask.Draw || !SketchEditor.IsEnabled || !SketchEditor.IsVisible)
            {
                if (MapView.ToolTip is ToolTip t)
                {
                    t.IsOpen = false;
                    MapView.ToolTip = null;
                }
                return;
            }
            if (SketchEditor.CreationMode
                    is SketchCreationMode.Arrow
                    or SketchCreationMode.Circle
                    or SketchCreationMode.Ellipse
                    or SketchCreationMode.Rectangle
                    or SketchCreationMode.Triangle
                    or SketchCreationMode.FreehandLine
                    or SketchCreationMode.FreehandPolygon)
            {
                return;
            }
            var position = e.GetPosition(MapView);
            if (!isSearchingNearestPoint)
            {
                isSearchingNearestPoint = true;

                (nearestVertex, nearestPoint) = await GetNearestVertexAndPointAsync(position);
                isSearchingNearestPoint = false;
                //因为查询需要一定时间，结束以后可能就不在绘制状态了，所以需要再次判断
                if (MapView.CurrentTask != BoardTask.Draw || !SketchEditor.IsEnabled || !SketchEditor.IsVisible)
                {
                    return;
                }
                if (Config.Instance.ShowNearestPointSymbol)
                {
                    MapView.Overlay.SetNearestVertexPoint(nearestVertex);
                    MapView.Overlay.SetNearestPointPoint(nearestPoint);
                }
                if (nearestVertex != null)
                {
                    if (MapView.ToolTip == null)
                    {
                        MapView.ToolTip = new ToolTip()
                        {
                            Content = "Ctrl + 右键：捕捉最近的结点" + (Config.Instance.ShowNearestPointSymbol ? "（红色）" : "")
                            + Environment.NewLine
                            + "Shift + 右键：捕捉最近的任意点" + (Config.Instance.ShowNearestPointSymbol ? "（黄色）" : ""),
                            IsOpen = true,
                            Placement = System.Windows.Controls.Primitives.PlacementMode.Relative
                        };
                    }
                }
                else
                {
                    if (MapView.ToolTip is ToolTip tt)
                    {
                        tt.IsOpen = false;
                        MapView.ToolTip = null;
                    }
                }
                if (MapView.ToolTip is ToolTip t)
                {
                    t.HorizontalOffset = position.X + 20;
                    t.VerticalOffset = position.Y - 40;
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
            if (SketchEditor.CreationMode
                     is SketchCreationMode.Arrow
                     or SketchCreationMode.Circle
                     or SketchCreationMode.Ellipse
                     or SketchCreationMode.Rectangle
                     or SketchCreationMode.Triangle
                     or SketchCreationMode.FreehandLine
                     or SketchCreationMode.FreehandPolygon)
            {
                return;
            }

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (nearestVertex != null)
                {
                    AddPointToSketchEditor(nearestVertex);
                }
                return;
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (nearestVertex != null)
                {
                    AddPointToSketchEditor(nearestPoint);
                }
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