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
            Edit = new EditHelper();
            Selection = new SelectionHelper();
            Drawing = new DrawHelper();
            Layer = new LayerHelper();
            Overlay = new OverlayHelper();
            ViewpointChanged += ArcMapView_ViewpointChanged;
            InteractionOptions = new MapViewInteractionOptions()
            {
                IsRotateEnabled = true
            };
            Load();
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
            if (GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry is Envelope envelope)
            {
                Model.LayerCollection.Instance.MapViewExtentJson = envelope.ToJson();
            }
        }

        public static ArcMapView Instance { get; private set; }
        public EditHelper Edit { get; }
        public SelectionHelper Selection { get; }
        public DrawHelper Drawing { get; }
        public LayerHelper Layer { get; }
        public OverlayHelper Overlay { get; set; }

        public async Task ZoomToGeometry(Geometry geometry, bool autoExtent = true)
        {
            await SetViewpointGeometryAsync(geometry, Config.Instance.HideWatermark && autoExtent ? Config.WatermarkHeight : 0);
        }

        /// <summary>
        /// 左键抬起事件，用于结束框选
        /// </summary>
        /// <param name="e"></param>
        protected async override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);
            if (SketchEditor.Geometry != null && BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
            {
                await Selection.StopFrameSelect(true);
            }
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
                       await FeatureUtility.DeleteAsync(Model.LayerCollection.Instance.Selected, ArcMapView.Instance.Selection.SelectedFeatures.ToArray());
                   }, true);
                    break;

                case Key.Space:
                case Key.Enter:
                    switch (BoardTaskManager.CurrentTask)
                    {
                        case BoardTaskManager.BoardTask.Draw:
                            await Drawing.StopDraw();
                            break;

                        case BoardTaskManager.BoardTask.Edit:
                            await Edit.StopAsync();
                            break;

                        case BoardTaskManager.BoardTask.Ready when Drawing.CurrentDrawMode.HasValue:
                            await Drawing.StartDraw(Drawing.CurrentDrawMode.Value);
                            break;
                    }
                    break;

                case Key.Escape when BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw:
                    await Drawing.StopDraw(false);
                    break;

                case Key.Escape when BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit:
                    await Edit.CancelEditingAsync();
                    break;

                case Key.Escape when Selection.SelectedFeatures.Count > 0:
                    await Selection.StopFrameSelect(false);
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

        /// <summary>
        /// 加载底图和图层事件
        /// </summary>
        /// <returns></returns>
        private async Task Load()
        {
            await LoadBasemap();
            if (Model.LayerCollection.Instance.MapViewExtentJson != null)
            {
                try
                {
                    await ZoomToGeometry(Envelope.FromJson(Model.LayerCollection.Instance.MapViewExtentJson), false);
                }
                catch
                {
                }
            }
        }

        public async Task LoadBasemap()
        {
            await GeoViewHelper.LoadBaseGeoViewAsync(this);
        }
    }
}