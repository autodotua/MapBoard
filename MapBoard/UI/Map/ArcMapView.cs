using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using MapBoard.Common;
using MapBoard.Common.UI;
using MapBoard.Main.UI.Model;
using MapBoard.Main.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MapBoard.Main.UI.Map
{
    public class ArcMapView : MapView, INotifyPropertyChanged
    {
        private static List<ArcMapView> instances = new List<ArcMapView>();
        public static IReadOnlyList<ArcMapView> Instances => instances.AsReadOnly();

        public ArcMapView()
        {
            instances.Add(this);
            AllowDrop = true;
            IsAttributionTextVisible = false;
            SketchEditor = new SketchEditor();
            ViewInsets = new Thickness(8);
            this.SetHideWatermark();

            InteractionOptions = new MapViewInteractionOptions()
            {
                IsRotateEnabled = true
            };
        }

        /// <summary>
        /// 画板当前任务
        /// </summary>
        private static BoardTask currentTask = BoardTask.Ready;

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
        public MapLayerCollection Layers { get; private set; }

        public async Task LoadAsync()
        {
            await GeoViewHelper.LoadBaseGeoViewAsync(this);
            Map.MaxScale = 100;
            Layers = await MapLayerCollection.GetInstanceAsync(Map.OperationalLayers);
            ZoomToLastExtent().ContinueWith(t => ViewpointChanged += ArcMapView_ViewpointChanged);
            Editor = new EditorHelper(this);
            Selection = new SelectionHelper(this);
            Overlay = new OverlayHelper(GraphicsOverlays, async p => await ZoomToGeometryAsync(p));
            Selection.CollectionChanged += Selection_CollectionChanged;
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

                case Key.Delete when CurrentTask == BoardTask.Select:
                    await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
                   {
                       await FeatureUtility.DeleteAsync(Layers.Selected, Selection.SelectedFeatures.ToArray());
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
                        when Editor.CurrentDrawMode.HasValue
                        && Layers.Selected != null
                        && Layers.Selected.LayerVisible:
                            await Editor.DrawAsync(Editor.CurrentDrawMode.Value);
                            break;
                    }
                    break;

                case Key.Escape when CurrentTask == BoardTask.Draw:
                    Editor.Cancel();
                    break;

                case Key.Escape when Selection.SelectedFeatures.Count > 0:
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