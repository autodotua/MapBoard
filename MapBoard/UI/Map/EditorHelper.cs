using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Main.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MapBoard.Main.UI.Map
{
    public class EditorHelper : INotifyPropertyChanged
    {
        public EditorHelper(SketchEditor sketch)
        {
            SketchEditor = sketch;
            sketch.GeometryChanged += Sketch_GeometryChanged;
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

        public SketchEditor SketchEditor { get; }

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
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Draw;
            Mode = EditMode.Creat;
            if (Attributes != null)
            {
                var label = Attributes.Label;
                var key = Attributes.Key;
                var date = Attributes.Date;
                Attributes = FeatureAttributes.Empty(MapLayerCollection.Instance.Selected);
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
                Attributes = FeatureAttributes.Empty(MapLayerCollection.Instance.Selected);
            }
            await SketchEditor.StartAsync(mode);
            if (geometry != null)
            {
                ShapefileFeatureTable table = MapLayerCollection.Instance.Selected.Table;

                Feature feature = table.CreateFeature();
                feature.Geometry = geometry;
                Attributes.SaveToFeature(feature);
                await table.AddFeatureAsync(feature);

                MapLayerCollection.Instance.Selected.NotifyFeatureChanged();
            }
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <returns></returns>
        public async Task EditAsync(LayerInfo layer, Feature feature)
        {
            Mode = EditMode.Edit;
            Attributes = FeatureAttributes.FromFeature(layer, feature);

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Draw;
            await SketchEditor.StartAsync(GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator));
            if (geometry != null)
            {
                feature.Geometry = geometry;
                Attributes.SaveToFeature(feature);
                await feature.FeatureTable.UpdateFeatureAsync(feature);
            }
        }

        /// <summary>
        /// 获取一条折线
        /// </summary>
        /// <returns></returns>
        public async Task<Polyline> GetPolylineAsync()
        {
            Mode = EditMode.GetLine;

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Draw;

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

        public async Task MeasureLength()
        {
            Mode = EditMode.MeasureLength;
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Draw;
            await SketchEditor.StartAsync(SketchCreationMode.Polyline);
        }

        public async Task MeasureArea()
        {
            Mode = EditMode.MeasureArea;
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Draw;
            await SketchEditor.StartAsync(SketchCreationMode.Polygon);
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
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }

        public async Task<Envelope> GetRectangleAsync()
        {
            Debug.WriteLine("开始绘制");
            Mode = EditMode.GetRectangle;

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Select;
            var geometry = await SketchEditor.StartAsync(SketchCreationMode.Rectangle, false);
            StopAndSave();
            if (geometry is Polygon rect)
            {
                return rect.Extent;
            }
            return null;
        }
    }
}