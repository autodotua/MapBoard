using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.UI;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MapBoard.Mapping
{
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

        public async Task LoadAsync()
        {
            await GeoViewHelper.LoadBaseGeoViewAsync(this);
            Layers = await MapLayerCollection.GetInstanceAsync(Scene.OperationalLayers);
            ZoomToLastExtent().ConfigureAwait(false);
            Overlay = new OverlayHelper(GraphicsOverlays, async p => await ZoomToGeometryAsync(p));
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

        public Task<MapPoint> GetPointAsync()
        {
            return SceneEditHelper.CreatePointAsync(this);
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

        public OverlayHelper Overlay { get; private set; }
        public MapLayerCollection Layers { get; private set; }
    }

    public class MainMapView : MapView, IMapBoardGeoView
    {
        private static List<MainMapView> instances = new List<MainMapView>();
        public static IReadOnlyList<MainMapView> Instances => instances.AsReadOnly();

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
        }

        private CancellationTokenSource ctsWfs = null;

        private async void MainMapView_NavigationCompleted(object sender, EventArgs e)
        {
            //加载WFS时，旧的结果会被抹掉，导致选中的图形会被取消选择。所以需要在非选择状态下进行。
            if (Layers.Any(p => p is IServerMapLayerInfo
            && !(p as IServerMapLayerInfo).AutoPopulateAll
            && !(p as IServerMapLayerInfo).HasPopulateAll)
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
                    foreach (IServerMapLayerInfo layer in Layers
                        .OfType<IServerMapLayerInfo>()
                        .Where(p => p.IsLoaded))
                    {
                        tasks.Add(layer.PopulateFromServiceAsync(visibleExtentQuery, false, cancellationToken: ctsWfs.Token));
                    }
                    await Task.WhenAll(tasks);
                    ctsWfs = null;
                }
                catch (TaskCanceledException ex)
                {
                }
                catch (HttpRequestException ex)
                {
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// 画板当前任务
        /// </summary>
        private static BoardTask currentTask = BoardTask.NotReady;

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

        public event EventHandler<BoardTaskChangedEventArgs> BoardTaskChanged;

        private double startRotation = 0;
        private Point startPosition = default;
        private bool canRotate = true;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                startRotation = MapRotation;
                startPosition = e.GetPosition(this);
            }
        }

        protected async override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (e.RightButton == MouseButtonState.Pressed)
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

        protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDoubleClick(e);
            if (e.ChangedButton.HasFlag(MouseButton.Right))
            {
                SetViewpointRotationAsync(0);
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

        public SelectionHelper Selection { get; private set; }
        public EditorHelper Editor { get; private set; }
        public OverlayHelper Overlay { get; private set; }
        public MapLayerCollection Layers { get; }

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

        public ItemsOperationErrorCollection BaseMapLoadErrors { get; private set; }

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

        public async Task ZoomToGeometryAsync(Geometry geometry, bool autoExtent = true)
        {
            if (geometry is MapPoint)
            {
                if (geometry.SpatialReference.Wkid != SpatialReferences.WebMercator.Wkid)
                {
                    geometry = GeometryEngine.Project(geometry, SpatialReferences.WebMercator);
                }
                geometry = GeometryEngine.Buffer(geometry, 100);
            }
            await SetViewpointGeometryAsync(geometry, Config.Instance.HideWatermark && autoExtent ? Config.WatermarkHeight : 0);
        }

        /// <summary>
        /// 键盘按下事件
        /// </summary>
        /// <param name="e"></param>
        protected async override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Delete when SketchEditor.SelectedVertex != null:
                    SketchEditor.RemoveSelectedVertex();
                    break;

                case Key.Delete when CurrentTask == BoardTask.Select && Layers.Selected is IEditableLayerInfo w:
                    await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
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
                        when Layers.Selected?.LayerVisible == true:
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
        /// 旧任务
        /// </summary>
        public BoardTask OldTask { get; private set; }

        /// <summary>
        /// 新任务
        /// </summary>
        public BoardTask NewTask { get; private set; }
    }
}