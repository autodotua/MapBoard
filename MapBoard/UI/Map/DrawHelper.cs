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


namespace MapBoard.UI.Map
{
    public class DrawHelper
    {

        public DrawHelper()
        {
        }

        public string Label { get; set; }
        public DateTimeOffset? Date { get; set; }
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
                ShapefileFeatureTable table = StyleCollection.Instance.Selected.Table;
                Feature feature = null;
                Geometry geometry = Mapview.SketchEditor.Geometry;
                //switch (Mapview.SketchEditor.CreationMode)
                //{
                //    case SketchCreationMode.Point:
                //        //case SketchCreationMode.Multipoint:
                //        // table = await GetFeatureTable(GeometryType.Point);
                //        if (Config.Instance.StaticEnable)
                //        {
                //            //table = await GetFeatureTable(GeometryType.Polygon);
                //            geometry = GeometryEngine.Buffer(geometry, Config.Instance.StaticWidth);
                //        }

                //        break;
                //    case SketchCreationMode.Multipoint:
                //        //case SketchCreationMode.Multipoint:
                //        //table = await GetFeatureTable(GeometryType.Multipoint);
                //        break;
                //    case SketchCreationMode.FreehandLine:
                //    case SketchCreationMode.Polyline:
                //        if (Config.Instance.StaticEnable)
                //        {
                //            //table = await GetFeatureTable(GeometryType.Polygon);
                //                geometry = GeometryEngine.Buffer(geometry, Config.Instance.StaticWidth);
                //        }
                //        else
                //        {
                //           // table = await GetFeatureTable(GeometryType.Polyline);

                //        }
                //        break;
                //    case SketchCreationMode.Circle:
                //    case SketchCreationMode.Ellipse:
                //    case SketchCreationMode.Arrow:
                //    case SketchCreationMode.Polygon:
                //    case SketchCreationMode.FreehandPolygon:
                //    case SketchCreationMode.Rectangle:
                //    case SketchCreationMode.Triangle:
                //       // table = await GetFeatureTable(GeometryType.Polygon);
                //        break;
                //}

                feature = table.CreateFeature();
                feature.Geometry = geometry;
                if (!string.IsNullOrWhiteSpace(Label))
                {
                    feature.Attributes[Resource.Resource.DisplayFieldName] = Label;
                }
                if (Date.HasValue)
                {
                    Date = new DateTimeOffset(Date.Value.DateTime,TimeSpan.Zero);
                    feature.Attributes[Resource.Resource.TimeExtentFieldName] = Date.Value.UtcDateTime;
                }
                else
                {
                    feature.Attributes[Resource.Resource.TimeExtentFieldName] = null;
                }
                await table.AddFeatureAsync(feature);

                StyleCollection.Instance.Styles.FirstOrDefault(p => p.Table == table).UpdateFeatureCount();

            }
            if (!Config.Instance.RemainLabel)
            {
                Label = null;
            }
            Mapview.SketchEditor.Stop();
            BoardTaskManager.CurrentTask = BoardTaskManager.BoardTask.Ready;
        }


        //private async Task<ShapefileFeatureTable> GetFeatureTable(GeometryType type)
        //{
        //    var style = StyleCollection.Instance.Selected;// StyleHelper.GetStyle(StyleCollection.Instance.Selected, type);
        //    ShapefileFeatureTable table = style.Table;

        //    if (table == null)
        //    {
        //        table = new ShapefileFeatureTable(Config.DataPath + "\\" + style.Name + ".shp");
        //        await table.LoadAsync();
        //        if (table.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
        //        {

        //            FeatureLayer layer = new FeatureLayer(table);
        //            //Map.OperationalLayers.Add(layer);
        //            style.Table = table;

        //            StyleHelper.ApplyStyles(style);
        //        }
        //    }
        //    return table;
        //}

    }
}
