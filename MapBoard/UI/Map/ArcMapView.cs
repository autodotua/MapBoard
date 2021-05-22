using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic;
using FzLib.Basic.Collection;
using FzLib.IO;
using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.Common.BaseLayer;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapBoard.Main.UI.Map
{
    public class ArcMapView : MapView, INotifyPropertyChanged
    {
        public ArcMapView()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new Exception("不允许多实例");
            }
            AllowDrop = true;
            IsAttributionTextVisible = false;
            SketchEditor = new SketchEditor();
            ViewInsets = new Thickness(8);
            this.SetHideWatermark();
            Selection = new SelectionHelper();
            Editor = new EditorHelper();
            Layer = new LayerHelper();
            Overlay = new OverlayHelper();
            ViewpointChanged += ArcMapView_ViewpointChanged;
            InteractionOptions = new MapViewInteractionOptions()
            {
                IsRotateEnabled = true
            };
        }

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
            if (MapLayerCollection.IsInstanceLoaded
                && GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry is Envelope envelope)
            {
                MapLayerCollection.Instance.MapViewExtentJson = envelope.ToJson();
            }
        }

        public static ArcMapView Instance { get; private set; }
        public SelectionHelper Selection { get; }
        public EditorHelper Editor { get; }
        public LayerHelper Layer { get; }
        public OverlayHelper Overlay { get; set; }

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

                case Key.Delete when BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select:
                    await (Window.GetWindow(this) as MainWindow).DoAsync(async () =>
                   {
                       await FeatureUtility.DeleteAsync(MapLayerCollection.Instance.Selected, Selection.SelectedFeatures.ToArray());
                       Selection.ClearSelection();
                   }, true);
                    break;

                case Key.Space:
                case Key.Enter:
                    switch (BoardTaskManager.CurrentTask)
                    {
                        case BoardTaskManager.BoardTask.Draw:
                        case BoardTaskManager.BoardTask.Edit:
                            Editor.StopAndSave();
                            break;

                        case BoardTaskManager.BoardTask.Ready
                        when Editor.CurrentDrawMode.HasValue
                        && MapLayerCollection.Instance.Selected != null
                        && MapLayerCollection.Instance.Selected.LayerVisible:
                            await Editor.DrawAsync(Editor.CurrentDrawMode.Value);
                            break;
                    }
                    break;

                case Key.Escape when BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit:
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
            if (MapLayerCollection.Instance.MapViewExtentJson != null)
            {
                try
                {
                    await ZoomToGeometryAsync(Envelope.FromJson(MapLayerCollection.Instance.MapViewExtentJson), false);
                }
                catch
                {
                }
            }
        }

        public async Task LoadBasemapAsync()
        {
            await GeoViewHelper.LoadBaseGeoViewAsync(this);
        }
    }
}