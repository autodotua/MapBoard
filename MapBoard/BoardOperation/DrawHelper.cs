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
using MapBoard.BoardOperation;
using MapBoard.Code;
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


namespace MapBoard.BoardOperation
{
    public class DrawHelper
    {

        public DrawHelper(ArcMapView mapview)
        {
            Mapview = mapview ?? throw new ArgumentNullException(nameof(mapview));
        }

   
        public ArcMapView Mapview { get; private set; }

        /// <summary>
        /// 开始绘制
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task StartDraw(SketchCreationMode mode)
        {
            await Mapview.SketchEditor.StartAsync(mode);
        }

        /// <summary>
        /// 绘制结束
        /// </summary>
        /// <returns></returns>
        public async Task StopDraw()
        {
            if (Mapview.SketchEditor.Geometry != null)
            {
                string fileName = "";
                ShapefileFeatureTable table = null;
                switch (Mapview.SketchEditor.CreationMode)
                {
                    case SketchCreationMode.Point:
                        //case SketchCreationMode.Multipoint:
                        table = await Mapview.GetFeatureTable(GeometryType.Point);
                        if (table != null)
                        {
                            MapPoint point = Mapview.SketchEditor.Geometry as MapPoint;
                            Feature feature = table.CreateFeature();
                            feature.Geometry = point;
                            await table.AddFeatureAsync(feature);
                        }
                        break;
                    case SketchCreationMode.Multipoint:
                        //case SketchCreationMode.Multipoint:
                        table = await Mapview. GetFeatureTable(GeometryType.Multipoint);
                        if (table != null)
                        {
                            Multipoint point = Mapview.SketchEditor.Geometry as Multipoint;
                            Feature feature = table.CreateFeature();
                            feature.Geometry = point;
                            await table.AddFeatureAsync(feature);
                        }
                        break;
                    case SketchCreationMode.FreehandLine:
                    case SketchCreationMode.Polyline:
                        if (Config.Instance.StaticEnable)
                        {
                            table = await Mapview.GetFeatureTable(GeometryType.Polygon);
                            if (table != null)
                            {
                                Polyline line = Mapview.SketchEditor.Geometry as Polyline;
                                Feature feature = table.CreateFeature();
                                feature.Geometry = GeometryEngine.Buffer(line, Config.Instance.StaticWidth);
                                await table.AddFeatureAsync(feature);
                            }
                        }
                        else
                        {
                            table = await Mapview.GetFeatureTable(GeometryType.Polyline);
                            if (table != null)
                            {
                                Polyline line = Mapview.SketchEditor.Geometry as Polyline;
                                Feature feature = table.CreateFeature();
                                feature.Geometry = line;
                                await table.AddFeatureAsync(feature);
                            }
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
                        if (table != null)
                        {
                            Polygon polygon = Mapview.SketchEditor.Geometry as Polygon;
                            Feature feature = table.CreateFeature();
                            feature.Geometry = polygon;
                            await table.AddFeatureAsync(feature);
                        }
                        break;
                }
                 StyleCollection.Instance.Styles.FirstOrDefault(p => p.Table == table).UpdateFeatureCount();

            }
            Mapview.SketchEditor.Stop();
        }



    }
}
