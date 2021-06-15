using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Main.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MapBoard.Main.UI.Map.Model;

namespace MapBoard.Main.UI.Map
{
    public class EditorHelper : INotifyPropertyChanged
    {
        public EditorHelper(ArcMapView mapView)
        {
            MapView = mapView;
            SketchEditor.GeometryChanged += Sketch_GeometryChanged;
        }

        private void Sketch_GeometryChanged(object sender, GeometryChangedEventArgs e)
        {
            GeometryChanged?.Invoke(sender, e);
        }

        public event EventHandler<GeometryChangedEventArgs> GeometryChanged;

        /// <summary>
        /// 正在编辑的要素的属性
        /// </summary>
        private FeatureAttributes attributes;

        /// <summary>
        /// 绘制完成的图形
        /// </summary>
        private Geometry geometry = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public enum EditMode
        {
            None,
            Creat,
            Edit,
            GetLine,
            GetPoint,
            GetRectangle,
            MeasureLength,
            MeasureArea
        }

        /// <summary>
        /// 正在编辑的要素的属性
        /// </summary>
        public FeatureAttributes Attributes
        {
            get => attributes;
            set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

        /// <summary>
        /// 当前草图编辑器的模式
        /// </summary>
        public SketchCreationMode? CurrentDrawMode { get; set; }

        /// <summary>
        /// 当前正在进行的绘制操作的类型
        /// </summary>
        public EditMode Mode { get; private set; }

        public SketchEditor SketchEditor => MapView.SketchEditor;
        public MapLayerCollection Layers => MapView.Layers;
        public ArcMapView MapView { get; }

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
            if (Attributes != null)
            {
                var label = Attributes.Label;
                var key = Attributes.Key;
                var date = Attributes.Date;
                Attributes = FeatureAttributes.Empty(Layers.Selected);
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
                Attributes = FeatureAttributes.Empty(Layers.Selected);
            }
            StartDraw(EditMode.Creat);

            await SketchEditor.StartAsync(mode);
            if (geometry != null)
            {
                Feature feature = Layers.Selected.CreateFeature();
                feature.Geometry = geometry;
                Attributes.SaveToFeature(feature);
                await Layers.Selected.AddFeatureAsync(feature, FeaturesChangedSource.Draw);
            }
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <returns></returns>
        public async Task EditAsync(MapLayerInfo layer, Feature feature)
        {
            Attributes = FeatureAttributes.FromFeature(layer, feature);
            StartDraw(EditMode.Edit);
            await SketchEditor.StartAsync(GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator));
            if (geometry != null)
            {
                UpdatedFeature newFeature = new UpdatedFeature(feature, feature.Geometry, new Dictionary<string, object>(feature.Attributes));
                feature.Geometry = geometry;
                Attributes.SaveToFeature(feature);
                await layer.UpdateFeatureAsync(newFeature, FeaturesChangedSource.Edit);
            }
        }

        /// <summary>
        /// 获取一条折线
        /// </summary>
        /// <returns></returns>
        public async Task<Polyline> GetPolylineAsync()
        {
            StartDraw(EditMode.GetLine);
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
        /// 获取一个点
        /// </summary>
        /// <returns></returns>
        public async Task<MapPoint> GetPointAsync()
        {
            StartDraw(EditMode.GetPoint);
            var geom = await SketchEditor.StartAsync(SketchCreationMode.Point, false);
            Cancel();
            if (geom is MapPoint point)
            {
                return point;
            }
            return null;
        }

        public async Task MeasureLength()
        {
            StartDraw(EditMode.MeasureLength);
            await SketchEditor.StartAsync(SketchCreationMode.Polyline);
        }

        public async Task MeasureArea()
        {
            StartDraw(EditMode.MeasureArea);
            await SketchEditor.StartAsync(SketchCreationMode.Polygon);
        }

        private void StartDraw(EditMode mode)
        {
            Mode = mode;
            MapView.CurrentTask = BoardTask.Draw;
            EditorStatusChanged?.Invoke(this, new EditorStatusChangedEventArgs(true));
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

        private void Stop()
        {
            Debug.WriteLine("停止绘制");
            Mode = EditMode.None;
            SketchEditor.Stop();
            EditorStatusChanged?.Invoke(this, new EditorStatusChangedEventArgs(false));
            MapView.CurrentTask = BoardTask.Ready;
        }

        public async Task<Envelope> GetRectangleAsync()
        {
            StartDraw(EditMode.GetRectangle);
            var geometry = await SketchEditor.StartAsync(SketchCreationMode.Rectangle, false);
            StopAndSave();
            if (geometry is Polygon rect)
            {
                return rect.Extent;
            }
            return null;
        }

        public event EventHandler<EditorStatusChangedEventArgs> EditorStatusChanged;
    }

    public class EditorStatusChangedEventArgs : EventArgs
    {
        public EditorStatusChangedEventArgs(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public bool IsRunning { get; set; }
    }
}