using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Basic;
using FzLib.Control.Dialog;
using FzLib.Geography.Format;
using MapBoard.Style;
using MapBoard.UI;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace MapBoard.UI.BoardOperation
{
    public class DrawHelper
    {

        public DrawHelper()
        {
        }

        public string Label { get; set; }
        private ArcMapView Mapview => ArcMapView.Instance;

        public SketchCreationMode? LastDrawMode { get; private set; }

        /// <summary>
        /// 开始绘制
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task StartDraw(SketchCreationMode mode)
        {
            LastDrawMode = mode;
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Draw;
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
                ShapefileFeatureTable table = null;
                Feature feature = null;
                Geometry geometry = Mapview.SketchEditor.Geometry;
                switch (Mapview.SketchEditor.CreationMode)
                {
                    case SketchCreationMode.Point:
                        //case SketchCreationMode.Multipoint:
                        table = await Mapview.GetFeatureTable(GeometryType.Point);

                        break;
                    case SketchCreationMode.Multipoint:
                        //case SketchCreationMode.Multipoint:
                        table = await Mapview.GetFeatureTable(GeometryType.Multipoint);
                        break;
                    case SketchCreationMode.FreehandLine:
                    case SketchCreationMode.Polyline:
                        if (Config.Instance.StaticEnable)
                        {
                            table = await Mapview.GetFeatureTable(GeometryType.Polygon);
                            if (table != null)
                            {
                                geometry = GeometryEngine.Buffer(geometry, Config.Instance.StaticWidth);
                            }
                        }
                        else
                        {
                            table = await Mapview.GetFeatureTable(GeometryType.Polyline);

                        }
                        break;
                    case SketchCreationMode.Circle:
                    case SketchCreationMode.Ellipse:
                    case SketchCreationMode.Arrow:
                    case SketchCreationMode.Polygon:
                    case SketchCreationMode.FreehandPolygon:
                    case SketchCreationMode.Rectangle:
                    case SketchCreationMode.Triangle:
                        table = await Mapview.GetFeatureTable(GeometryType.Polygon);
                        break;
                }

                if (table != null)
                {
                    feature = table.CreateFeature();
                    feature.Geometry = geometry;
                    if(!string.IsNullOrEmpty(Label))
                    {
                        feature.Attributes["Info"] = Label;
                    }
                    await table.AddFeatureAsync(feature);
                }

                StyleCollection.Instance.Styles.FirstOrDefault(p => p.Table == table).UpdateFeatureCount();

            }
            Mapview.SketchEditor.Stop();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }



    }
}
