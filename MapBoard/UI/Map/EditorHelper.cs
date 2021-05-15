using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Common.Resource;
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
            Creat,
            Edit,
            GetLine,
            GetRectangle,
            Ready
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

        private ArcMapView Mapview => ArcMapView.Instance;

        /// <summary>
        /// 停止并不保存
        /// </summary>
        public void Cancel()
        {
            if (Mode == EditMode.Ready)
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
                if (!Config.Instance.RemainLabel)
                {
                    Attributes.Label = "";
                }
                if (!Config.Instance.RemainKey)
                {
                    Attributes.Key = "";
                }
                if (!Config.Instance.RemainDate)
                {
                    Attributes.Date = null;
                }
            }
            else
            {
                Attributes = FeatureAttributes.Empty;
            }
            await Mapview.SketchEditor.StartAsync(mode);
            if (geometry != null)
            {
                ShapefileFeatureTable table = LayerCollection.Instance.Selected.Table;

                Feature feature = table.CreateFeature();
                feature.Geometry = geometry;
                Attributes.SaveToFeature(feature);
                await table.AddFeatureAsync(feature);

                LayerCollection.Instance.Selected.UpdateFeatureCount();
            }
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <returns></returns>
        public async Task EditAsync(Feature feature)
        {
            Mode = EditMode.Edit;
            Mapview.Editor.Attributes = FeatureAttributes.FromFeature(feature);

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Edit;
            await Mapview.SketchEditor.StartAsync(GeometryEngine.Project(feature.Geometry, SpatialReferences.WebMercator));
            if (geometry != null)
            {
                feature.Geometry = geometry;
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

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Edit;

            await Mapview.SketchEditor.StartAsync(SketchCreationMode.Polyline);
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
        /// 停止并保存当前结果
        /// </summary>
        public void StopAndSave()
        {
            if (Mode == EditMode.Ready)
            {
                return;
            }
            geometry = Mapview.SketchEditor.Geometry;
            Stop();
        }

        private void Stop()
        {
            Debug.WriteLine("停止绘制");
            Mode = EditMode.Ready;
            Mapview.SketchEditor.Stop();
            Mapview.Selection.ClearSelection();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }

        public async Task<Envelope> GetRectangleAsync()
        {
            Debug.WriteLine("开始绘制");
            Mode = EditMode.GetRectangle;

            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Select;
            var geometry = await Mapview.SketchEditor.StartAsync(SketchCreationMode.Rectangle, false);
            StopAndSave();
            if (geometry is Polygon rect)
            {
                return rect.Extent;
            }
            return null;
        }
    }
}