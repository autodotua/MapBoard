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

namespace MapBoard.Mapping
{
    public class EditorHelper : INotifyPropertyChanged
    {
        public EditorHelper(MainMapView mapView)
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
        private FeatureAttributeCollection attributes;

        /// <summary>
        /// 绘制完成的图形
        /// </summary>
        private Geometry geometry = null;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 正在编辑的要素的属性
        /// </summary>
        public FeatureAttributeCollection Attributes
        {
            get => attributes;
            set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

        /// <summary>
        /// 当前正在进行的绘制操作的类型
        /// </summary>
        public EditMode Mode { get; private set; }

        public SketchEditor SketchEditor => MapView.SketchEditor;
        public MapLayerCollection Layers => MapView.Layers;
        public MainMapView MapView { get; }

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
            if (!Layers.Selected.IsEditable)
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
            Mode = EditMode.None;
            SketchEditor.Stop();
            EditorStatusChanged?.Invoke(this, new EditorStatusChangedEventArgs(false));
            MapView.CurrentTask = BoardTask.Ready;
        }

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
        }    /// <summary>

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

        public event EventHandler<EditorStatusChangedEventArgs> EditorStatusChanged;
    }

    public enum EditMode
    {
        None,
        Creat,
        Edit,
        GetGeometry,
        MeasureLength,
        MeasureArea
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