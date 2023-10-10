﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Maui;
using Esri.ArcGISRuntime.UI.Editing;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
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
using Esri.ArcGISRuntime.Symbology;
using NGettext.Loaders;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 主地图画板地图
    /// </summary>
    public class MainMapView : MapView
    {
        /// <summary>
        /// 画板当前任务
        /// </summary>
        private static BoardTask currentTask = BoardTask.NotReady;

        /// <summary>
        /// 所有<see cref="MainMapView"/>实例
        /// </summary>
        private static List<MainMapView> instances = new List<MainMapView>();

        private readonly double watermarkHeight = 72;
        /// <summary>
        /// 是否允许旋转
        /// </summary>
        private bool canRotate = true;


        private bool isZoomingToLastExtent = false;
        /// <summary>
        /// 鼠标中键按下时起始位置
        /// </summary>
        private Point startPosition = default;

        /// <summary>
        /// 旋转开始角度
        /// </summary>
        private double startRotation = 0;

        public MainMapView()
        {
            if (instances.Count > 1)
            {
                throw new Exception("该类仅支持单例");
            }
            instances.Add(this);
            Layers = new MapLayerCollection();

            IsAttributionTextVisible = false;
            Margin = new Thickness(-watermarkHeight);

            InteractionOptions = new MapViewInteractionOptions()
            {
                IsRotateEnabled = true
            };
            PropertyChanged += MainMapView_PropertyChanged;

            //启动时恢复到原来的视角，并定时保存
            Loaded += MainMapView_Loaded;
            Unloaded += MainMapView_Unloaded;
            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (IsLoaded
                    && Layers != null
                 && GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry is Envelope envelope)
                {
                    Layers.MapViewExtentJson = envelope.ToJson();
                }
                return true;
            });
            //NavigationCompleted += MainMapView_NavigationCompleted;
        }

        /// <summary>
        /// 画板当前任务改变事件
        /// </summary>
        public event EventHandler<BoardTaskChangedEventArgs> BoardTaskChanged;

        public static MainMapView Current => instances[0];

        /// <summary>
        /// 所有<see cref="MainMapView"/>实例
        /// </summary>
        public static IReadOnlyList<MainMapView> Instances => instances.AsReadOnly();

        /// <summary>
        /// 底图加载错误
        /// </summary>
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

        /// <summary>
        /// 图层
        /// </summary>
        public MapLayerCollection Layers { get; }

        /// <summary>
        /// 编辑器相关
        /// </summary>
        //public EditorHelper Editor { get; private set; }
        public GraphicsOverlay TrackOverlay { get; } = new GraphicsOverlay();

        /// <summary>
        /// 选择相关
        /// </summary>
        //public SelectionHelper Selection { get; private set; }
        /// <summary>
        /// 初始化加载
        /// </summary>
        /// <returns></returns>
        public async Task LoadAsync()
        {
            BaseMapLoadErrors = await MapViewHelper.LoadBaseGeoViewAsync(this, Config.Instance.EnableBasemapCache);
            Map.MaxScale = Config.Instance.MaxScale;
            await Layers.LoadAsync(Map.OperationalLayers);
            CurrentTask = BoardTask.Ready;

            if (!GraphicsOverlays.Contains(TrackOverlay))
            {
                TrackOverlay.Renderer = new SimpleRenderer(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.FromArgb(0x54, 0xA5, 0xF6), 6));
                GraphicsOverlays.Add(TrackOverlay);

            }
        }

        public void MoveToLocation()
        {
            if (LocationDisplay != null && LocationDisplay.IsEnabled)
            {
                var point = LocationDisplay.MapLocation;
                if (point != null)
                {
                    SetViewpointCenterAsync(point);
                }
            }
        }
        private async void MainMapView_Loaded(object sender, EventArgs e)
        {
            if (Map == null)
            {
                await LoadAsync();
            }
            if (!isZoomingToLastExtent)
            {
                isZoomingToLastExtent = true;
                await this.TryZoomToLastExtent();
                isZoomingToLastExtent = false;
            }
        }

        private void MainMapView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocationDisplay))
            {
                if (LocationDisplay != null)
                {
                    LocationDisplay.NavigationPointHeightFactor = 0.4;
                    LocationDisplay.WanderExtentFactor = 0;
                    LocationDisplay.IsEnabled = true;
                }
            }
        }

        private void MainMapView_Unloaded(object sender, EventArgs e)
        {
            Config.Instance.Save();
            Layers.Save();
        }
        /// <summary>
        /// 覆盖层相关
        /// </summary>
        //public OverlayHelper Overlay { get; private set; }
        /// <summary>
        /// 设置设备位置的显示
        /// </summary>


        ///// <summary>
        ///// 鼠标移动，中键按下时旋转地图
        ///// </summary>
        ///// <param name="e"></param>
        //protected override async void OnPreviewMouseMove(MouseEventArgs e)
        //{
        //    base.OnPreviewMouseMove(e);
        //    if (e.MiddleButton == MouseButtonState.Pressed && canRotate)
        //    {
        //        Point position = e.GetPosition(this);
        //        double distance = position.X - startPosition.X;
        //        if (Math.Abs(distance) < 10)
        //        {
        //            return;
        //        }
        //        Debug.WriteLine(distance);
        //        canRotate = false;
        //        SetViewpointRotationAsync(startRotation + distance / 5);
        //        await Task.Delay(100);
        //        canRotate = true;
        //        //防止旋转过快造成卡顿
        //    }
        //}
        /// <summary>
        /// 键盘按下事件
        /// </summary>
        /// <param name="e"></param>
        //protected override async void OnPreviewKeyDown(KeyEventArgs e)
        //{
        //    base.OnPreviewKeyDown(e);
        //    switch (e.Key)
        //    {
        //        //Delete：移除节点、删除要素
        //        case Key.Delete when GeometryEditor.SelectedElement is GeometryEditorVertex:
        //            GeometryEditor.DeleteSelectedElement();
        //            break;

        //        case Key.Delete when CurrentTask == BoardTask.Select && Layers.Selected is IEditableLayerInfo w:
        //            await (this.GetWindow() as MainWindow).DoAsync(async () =>
        //           {
        //               await FeatureUtility.DeleteAsync(w, Selection.SelectedFeatures.ToArray());
        //               Selection.ClearSelection();
        //           }, "正在删除", true);
        //            break;

        //        //空格、回车：开始/结束绘图、选择模式下开始编辑
        //        case Key.Space:
        //        case Key.Enter:
        //            switch (CurrentTask)
        //            {
        //                case BoardTask.Draw:
        //                    Editor.StopAndSave();
        //                    break;

        //                case BoardTask.Ready
        //                when Layers.Selected?.LayerVisible == true
        //                && Layers.Selected?.Interaction?.CanEdit == true:
        //                    await Editor.DrawAsync(Layers.Selected.GeometryType, null);
        //                    break;

        //                case BoardTask.Select
        //                when Selection.SelectedFeatures.Count == 1
        //                    && Layers.Selected?.LayerVisible == true
        //                    && Layers.Selected?.CanEdit == true:
        //                    var feature = Selection.SelectedFeatures.Single();
        //                    Selection.ClearSelection();
        //                    Editor.EditAsync(Layers.Selected as IEditableLayerInfo, feature);
        //                    break;
        //            }
        //            break;

        //        //ESC：退出当前状态，返回就绪状态
        //        case Key.Escape
        //        when CurrentTask == BoardTask.Draw:
        //            Editor.Cancel();
        //            break;

        //        case Key.Escape
        //        when Selection.SelectedFeatures.Count > 0:
        //            Selection.ClearSelection();
        //            break;

        //        //Ctrl Z：撤销
        //        case Key.Z when Keyboard.Modifiers == ModifierKeys.Control && GeometryEditor.CanUndo:
        //            GeometryEditor.Undo();
        //            break;

        //        //Ctrl SHift Z/Ctrl Y：重做
        //        case Key.Z when Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && GeometryEditor.CanRedo:
        //            GeometryEditor.Redo();
        //            break;
        //        case Key.Y when Keyboard.Modifiers == ModifierKeys.Control && GeometryEditor.CanRedo:
        //            GeometryEditor.Redo();
        //            break;
        //    }
        //}

        //protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
        //{
        //    base.OnPreviewMouseDoubleClick(e);
        //    if (e.ChangedButton.HasFlag(MouseButton.Middle))
        //    {
        //        SetViewpointRotationAsync(0);
        //    }
        //}

        //protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        //{
        //    base.OnPreviewMouseDown(e);
        //    if (e.MiddleButton == MouseButtonState.Pressed)
        //    {
        //        startRotation = MapRotation;
        //        startPosition = e.GetPosition(this);
        //    }
        //}
        private void Selection_CollectionChanged(object sender, EventArgs e)
        {
            //if (Selection.SelectedFeatures.Count > 0)
            //{
            //    Layer selectionLayer = Selection.SelectedFeatures.First().FeatureTable.Layer;
            //    if (selectionLayer != Layers.Selected.Layer)
            //    {
            //        Layers.Selected = Layers.FirstOrDefault(p => (p as MapLayerInfo).Layer == selectionLayer) as MapLayerInfo;
            //    }
            //}
        }
    }
}