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
            SetHideWatermark();
            Edit = new EditHelper();
            Selection = new SelectionHelper();
            Drawing = new DrawHelper();
            Layer = new LayerHelper();
            Load().Wait();
        }

        public void SetHideWatermark()
        {
            Margin = new Thickness(Config.Instance.HideWatermark ? -72 : 0);
        }

        public static ArcMapView Instance { get; private set; }
        public EditHelper Edit { get; }
        public SelectionHelper Selection { get; }
        public DrawHelper Drawing { get; }
        public LayerHelper Layer { get; }

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
        /// 右键按下事件，用于显示右键菜单，现已废除
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonDown(e);
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
                    await Edit.DeleteSelectedFeatures();
                    break;

                case Key.Space:
                case Key.Enter:
                    switch (BoardTaskManager.CurrentTask)
                    {
                        case BoardTaskManager.BoardTask.Draw:
                            await Drawing.StopDraw();
                            break;

                        case BoardTaskManager.BoardTask.Edit:
                            await Edit.StopEditing();
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
                    await Edit.AbandonEditing();
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
        }

        public async Task LoadBasemap()
        {
            await GeoViewHelper.LoadBaseGeoViewAsync(this);
        }
    }
}