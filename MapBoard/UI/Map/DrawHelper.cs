using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Common.Resource;
using MapBoard.Main.Model;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MapBoard.Main.UI.Map
{
    public class DrawHelper : INotifyPropertyChanged
    {
        public DrawHelper()
        {
        }

        private FeatureAttributes attributes;

        public event PropertyChangedEventHandler PropertyChanged;

        public FeatureAttributes Attributes
        {
            get => attributes;
            set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

        private ArcMapView Mapview => ArcMapView.Instance;

        public SketchCreationMode? CurrentDrawMode { get; set; }

        /// <summary>
        /// 开始绘制
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task StartDraw(SketchCreationMode mode)
        {
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Draw;
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
            await Mapview.SketchEditor.StartAsync(mode);
        }

        /// <summary>
        /// 绘制结束
        /// </summary>
        /// <returns></returns>
        public async Task StopDraw(bool save = true)
        {
            if (Mapview.SketchEditor.Geometry != null && save)
            {
                string fileName = "";
                ShapefileFeatureTable table = LayerCollection.Instance.Selected.Table;
                Feature feature = null;
                Geometry geometry = Mapview.SketchEditor.Geometry;

                feature = table.CreateFeature();
                feature.Geometry = geometry;
                attributes.SaveToFeature(feature);
                await table.AddFeatureAsync(feature);

                LayerCollection.Instance.Layers.FirstOrDefault(p => p.Table == table).UpdateFeatureCount();
            }
            Mapview.SketchEditor.Stop();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }
    }
}