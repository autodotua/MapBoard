using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib;
using FzLib.WPF;
using FzLib.WPF.Dialog;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.UI;
using MapBoard.Util;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MapBoard.Mapping
{
    /// 画板任务类型
    /// </summary>
    public enum BoardTask
    {
        NotReady,
        Ready,
        Draw,
        Select,
    }

    /// <summary>
    /// 画板任务改变事件参数
    /// </summary>
    public class BoardTaskChangedEventArgs : EventArgs
    {
        public BoardTaskChangedEventArgs(BoardTask oldTask, BoardTask newTask)
        {
            OldTask = oldTask;
            NewTask = newTask;
        }

        /// <summary>
        /// 新任务
        /// </summary>
        public BoardTask NewTask { get; private set; }

        /// <summary>
        /// 旧任务
        /// </summary>
        public BoardTask OldTask { get; private set; }
    }

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

        public MapLayerCollection Layers { get; private set; }

        public OverlayHelper Overlay { get; private set; }

        public Task<MapPoint> GetPointAsync()
        {
            return SceneEditHelper.CreatePointAsync(this);
        }

        public async Task LoadAsync()
        {
            await GeoViewHelper.LoadBaseGeoViewAsync(this);
            Layers = await MapLayerCollection.GetInstanceAsync(Scene.OperationalLayers);
            ZoomToLastExtent().ConfigureAwait(false);
            Overlay = new OverlayHelper(GraphicsOverlays, async p => await ZoomToGeometryAsync(p));
        }

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

        public async Task ZoomToLastExtent()
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

    [DoNotNotify]
    public class MainMapView : MapView, IMapBoardGeoView
    {
        /// <summary>
        /// 画板当前任务
        /// </summary>
        private static BoardTask currentTask = BoardTask.NotReady;

        private static List<MainMapView> instances = new List<MainMapView>();
        private bool canRotate = true;
        private CancellationTokenSource ctsWfs = null;
        private Point startPosition = default;
        private double startRotation = 0;

        public MainMapView()
        {
            instances.Add(this);
            Layers = new MapLayerCollection();
            AllowDrop = true;
            IsAttributionTextVisible = false;
            SketchEditor = new SketchEditor();
            this.SetHideWatermark();

            InteractionOptions = new MapViewInteractionOptions()
            {
                IsRotateEnabled = true
            };
            NavigationCompleted += MainMapView_NavigationCompleted;
            SetLocationDisplay();
            Config.Instance.PropertyChanged += Config_PropertyChanged;
        }

        public event EventHandler<BoardTaskChangedEventArgs> BoardTaskChanged;

        public static IReadOnlyList<MainMapView> Instances => instances.AsReadOnly();
        public ItemsOperationErrorCollection BaseMapLoadErrors { get; private set; }

        /// <summary>
        /// 画板当前任务
        /// </summary>
        public BoardTask CurrentTask
        {
            get => currentTask;
            set
            {
                if (currentTask != value)
                {
                    BoardTask oldTask = currentTask;
                    currentTask = value;

                    BoardTaskChanged?.Invoke(null, new BoardTaskChangedEventArgs(oldTask, value));
                }
            }
        }

        public EditorHelper Editor { get; private set; }

        public MapLayerCollection Layers { get; }

        public OverlayHelper Overlay { get; private set; }

        public SelectionHelper Selection { get; private set; }

        public async Task LoadAsync()
        {
            BaseMapLoadErrors = await GeoViewHelper.LoadBaseGeoViewAsync(this);
            Map.MaxScale = Config.Instance.MaxScale;
            await Layers.LoadAsync(Map.OperationalLayers);
            ZoomToLastExtent().ContinueWith(t => ViewpointChanged += ArcMapView_ViewpointChanged);
            Editor = new EditorHelper(this);
            Selection = new SelectionHelper(this);
            Overlay = new OverlayHelper(GraphicsOverlays, async p => await ZoomToGeometryAsync(p));
            Selection.CollectionChanged += Selection_CollectionChanged;
            CurrentTask = BoardTask.Ready;
        }

        public void SetLocationDisplay()
        {
            LocationDisplay.ShowLocation = Config.Instance.ShowLocation;
            LocationDisplay.IsEnabled = Config.Instance.ShowLocation;
        }

        public async Task ZoomToGeometryAsync(Geometry geometry, bool autoExtent = true)
        {
            if (geometry is MapPoint || geometry is Multipoint m && m.Points.Count == 1)
            {
                if (geometry.SpatialReference.Wkid != SpatialReferences.WebMercator.Wkid)
                {
                    geometry = GeometryEngine.Project(geometry, SpatialReferences.WebMercator);
                }
                geometry = GeometryEngine.Buffer(geometry, 100);
            }
            var extent = geometry.Extent;
            if (double.IsNaN(extent.Width) || double.IsNaN(extent.Height) || extent.Width == 0 || extent.Height == 0)
            {
                SnakeBar.ShowError("图形为空");
                return;
            }
            await SetViewpointGeometryAsync(geometry, Config.Instance.HideWatermark && autoExtent ? Config.WatermarkHeight : 0);
        }

        public async Task ZoomToLastExtent()
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

        /// <summary>
        /// 键盘按下事件
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Delete when SketchEditor.SelectedVertex != null:
                    SketchEditor.RemoveSelectedVertex();
                    break;

                case Key.Delete when CurrentTask == BoardTask.Select && Layers.Selected is IEditableLayerInfo w:
                    await (this.GetWindow() as MainWindow).DoAsync(async () =>
                   {
                       await FeatureUtility.DeleteAsync(w, Selection.SelectedFeatures.ToArray());
                       Selection.ClearSelection();
                   }, "正在删除", true);
                    break;

                case Key.Space:
                case Key.Enter:
                    switch (CurrentTask)
                    {
                        case BoardTask.Draw:
                            Editor.StopAndSave();
                            break;

                        case BoardTask.Ready
                        when Layers.Selected?.LayerVisible == true
                        && Layers.Selected?.Interaction?.CanEdit == true:
                            SketchCreationMode? type = Layers.Selected.GeometryType switch
                            {
                                GeometryType.Point => SketchCreationMode.Point,
                                GeometryType.Multipoint => SketchCreationMode.Multipoint,
                                GeometryType.Polyline => SketchCreationMode.Polyline,
                                GeometryType.Polygon => SketchCreationMode.Polygon,
                                _ => null
                            };
                            if (type.HasValue)
                            {
                                await Editor.DrawAsync(type.Value);
                            }
                            break;

                        case BoardTask.Select
                        when Selection.SelectedFeatures.Count == 1
                            && Layers.Selected?.LayerVisible == true
                            && Layers.Selected?.CanEdit == true:
                            var feature = Selection.SelectedFeatures.Single();
                            Selection.ClearSelection();
                            Editor.EditAsync(Layers.Selected as IEditableLayerInfo, feature);
                            break;
                    }
                    break;

                case Key.Escape
                when CurrentTask == BoardTask.Draw:
                    Editor.Cancel();
                    break;

                case Key.Escape
                when Selection.SelectedFeatures.Count > 0:
                    Selection.ClearSelection();
                    break;

                case Key.Z when Keyboard.Modifiers == ModifierKeys.Control && SketchEditor.UndoCommand.CanExecute(null):
                    SketchEditor.UndoCommand.Execute(null);
                    break;

                case Key.Z when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && SketchEditor.RedoCommand.CanExecute(null):
                    SketchEditor.RedoCommand.Execute(null);
                    break;

                case Key.Y when Keyboard.Modifiers == ModifierKeys.Control && SketchEditor.RedoCommand.CanExecute(null):
                    SketchEditor.RedoCommand.Execute(null);
                    break;
            }
        }

        protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDoubleClick(e);
            if (e.ChangedButton.HasFlag(MouseButton.Right))
            {
                SetViewpointRotationAsync(0);
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                startRotation = MapRotation;
                startPosition = e.GetPosition(this);
            }
        }

        protected override async void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (e.RightButton == MouseButtonState.Pressed && CurrentTask == BoardTask.Ready)
            {
                if (!canRotate)
                {
                    return;
                }
                Point position = e.GetPosition(this);
                double distance = position.X - startPosition.X;
                if (Math.Abs(distance) < 10)
                {
                    return;
                }
                canRotate = false;
                SetViewpointRotationAsync(startRotation + distance / 5);
                await Task.Delay(100);
                canRotate = true;
                //防止旋转过快造成卡顿
            }
        }

        private void ArcMapView_ViewpointChanged(object sender, EventArgs e)
        {
            if (Layers != null
                && GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry is Envelope envelope)
            {
                Layers.MapViewExtentJson = envelope.ToJson();
            }
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.ShowLocation))
            {
                SetLocationDisplay();
            }
        }

        private async void MainMapView_NavigationCompleted(object sender, EventArgs e)
        {
            //加载WFS时，旧的结果会被抹掉，导致选中的图形会被取消选择。所以需要在非选择状态下进行。
            if (Layers.Any(p => p is IServerBasedLayer
            && !(p as IServerBasedLayer).AutoPopulateAll
            && !(p as IServerBasedLayer).HasPopulateAll)
                && CurrentTask == BoardTask.Ready)
            {
                Envelope currentExtent = VisibleArea.Extent;

                // Create a query based on the current visible extent.
                QueryParameters visibleExtentQuery = new QueryParameters
                {
                    Geometry = currentExtent,
                    SpatialRelationship = SpatialRelationship.Intersects
                };
                if (ctsWfs != null)
                {
                    ctsWfs.Cancel();
                }
                ctsWfs = new CancellationTokenSource();
                try
                {
                    List<Task> tasks = new();
                    foreach (IServerBasedLayer layer in Layers
                        .OfType<IServerBasedLayer>()
                        .Where(p => p.IsLoaded))
                    {
                        tasks.Add(layer.PopulateFromServiceAsync(visibleExtentQuery, false, cancellationToken: ctsWfs.Token));
                    }
                    await Task.WhenAll(tasks);
                    ctsWfs = null;
                }
                catch (TaskCanceledException ex)
                {
                    App.Log.Error(ex);
                }
                catch (HttpRequestException ex)
                {
                    App.Log.Error(ex);
                }
                catch (Exception ex)
                {
                    App.Log.Error(ex);
                }
            }
        }

        private void Selection_CollectionChanged(object sender, EventArgs e)
        {
            if (Selection.SelectedFeatures.Count > 0)
            {
                Layer selectionLayer = Selection.SelectedFeatures.First().FeatureTable.Layer;
                if (selectionLayer != Layers.Selected.Layer)
                {
                    Layers.Selected = Layers.FirstOrDefault(p => (p as MapLayerInfo).Layer == selectionLayer) as MapLayerInfo;
                }
            }
        }
    }
}