using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using MapBoard.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MapBoard.IO
{
    public static class GeoJson
    {
        public async static Task<JObject> ConvertAsync(IMapLayerInfo layer)
        {
            var features = await layer.GetAllFeaturesAsync();
            JObject result = null;
            await Task.Run(() => result = Convert(features));
            return result;
        }

        /// <summary>
        /// 将要素集合异步导出到GeoJSON
        /// </summary>
        /// <param name="path"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public async static Task<string> ExportAsync(string path, IEnumerable<Feature> features)
        {
            string result = null;
            await Task.Run(() =>
            {
                result = Convert(features).ToString();
                File.WriteAllText(path, result, new UTF8Encoding(true));
            });

            return result;
        }

        /// <summary>
        /// 将要素集合异步导出到GeoJSON
        /// </summary>
        /// <param name="path"></param>
        /// <param name="features"></param>
        /// <returns></returns>
        public async static Task<string> ExportWithStyleAsync(string path, IEnumerable<Feature> features, ILayerInfo layer)
        {
            string result = null;
            await Task.Run(() =>
            {
                var json = Convert(features);
                var s = new JsonSerializer();
                s.Converters.Add(new GeoJsonStyleConverter());
                json.Add("style", JObject.FromObject(layer.Renderer.DefaultSymbol, s));
                result = json.ToString(Formatting.Indented);
                File.WriteAllText(path, result, new UTF8Encoding(true));
            });

            return result;
        }

        /// <summary>
        /// 将图层异步导出到GeoJSON
        /// </summary>
        /// <param name="path"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public async static Task ExportAsync(string path, IMapLayerInfo layer)
        {
            var features = await layer.GetAllFeaturesAsync();
            await ExportAsync(path, features);
        }

        /// <summary>
        /// 将图层异步导出到GeoJSON
        /// </summary>
        /// <param name="path"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public async static Task ExportWithStyleAsync(string path, IMapLayerInfo layer)
        {
            var features = await layer.GetAllFeaturesAsync();
            await ExportWithStyleAsync(path, features, layer);
        }

        /// <summary>
        /// 将一组Feature对象转换为表示GeoJSON FeatureCollection的JObject
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        private static JObject Convert(IEnumerable<Feature> features)
        {
            JObject jRoot = new JObject
            {
                { "type", "FeatureCollection" }
            };
            JArray jFeatures = new JArray();
            jRoot.Add("features", jFeatures);

            foreach (var f in features)
            {
                JObject jF = new JObject();
                jFeatures.Add(jF);
                jF.Add("type", "Feature");
                var g = GetGeometryJson(f);
                if (g == null)
                {
                    continue;
                }
                jF.Add("geometry", g);
                jF.Add("properties", GetPropertiesJson(f));
            }
            return jRoot;
        }

        /// <summary>
        /// 将要素中的图形转为Json
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private static JObject GetGeometryJson(Feature f)
        {
            JObject jGeo = new JObject();
            Geometry g = f.Geometry;
            if (g.IsEmpty)
            {
                return null;
            }
            if (f.Geometry.SpatialReference != null && f.Geometry.SpatialReference.Wkid != 4326)
            {
                g = GeometryEngine.Project(g, SpatialReferences.Wgs84);
            }
            switch (g.GeometryType)
            {
                case GeometryType.Polygon:
                    jGeo.Add("type", "Polygon");
                    jGeo.Add("coordinates", GetPolygonOrMultiLineStringJson(g as Polygon));
                    break;

                case GeometryType.Polyline when (f.Geometry as Polyline).Parts.Count == 1:
                    jGeo.Add("type", "LineString");
                    jGeo.Add("coordinates", GetLineStringJson(g as Polyline));
                    break;

                case GeometryType.Polyline when (f.Geometry as Polyline).Parts.Count > 1:
                    jGeo.Add("type", "MultiLineString");
                    jGeo.Add("coordinates", GetPolygonOrMultiLineStringJson(g as Polyline));
                    break;

                case GeometryType.Polyline when (f.Geometry as Polyline).Parts.Count == 0:
                    return null;

                case GeometryType.Point:
                    jGeo.Add("type", "Point");
                    jGeo.Add("coordinates", GetPointJson(g as MapPoint));
                    break;

                case GeometryType.Multipoint:
                    jGeo.Add("type", "MultiPoint");
                    jGeo.Add("coordinates", GetMultiPointJson(g as Multipoint));
                    break;

                default:
                    throw new NotSupportedException("不支持的图形类型");
            }
            return jGeo;
        }

        /// <summary>
        /// 获取<see cref="Polyline"/>的GeoJson
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        private static JArray GetLineStringJson(Polyline g)
        {
            Debug.Assert(g.Parts.Count == 1);
            JArray jLine = new JArray();
            foreach (var point in g.Parts[0].Points)
            {
                var jPoint = new JArray();
                jPoint.Add(point.X);
                jPoint.Add(point.Y);
                jLine.Add(jPoint);
            }
            return jLine;
        }

        /// <summary>
        /// 获取<see cref="Multipoint"/>的GeoJson
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private static JArray GetMultiPointJson(Multipoint point)
        {
            JArray jPoints = new JArray();
            foreach (var p in point.Points)
            {
                JArray jPoint = new JArray();
                jPoint.Add(p.X);
                jPoint.Add(p.Y);
                jPoints.Add(jPoint);
            }
            return jPoints;
        }

        /// <summary>
        /// 获取<see cref="MapPoint"/>的GeoJson
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private static JArray GetPointJson(MapPoint point)
        {
            JArray jPoint = new JArray();
            jPoint.Add(point.X);
            jPoint.Add(point.Y);
            return jPoint;
        }

        /// <summary>
        /// 获取<see cref="Multipart"/>的GeoJson
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        private static JArray GetPolygonOrMultiLineStringJson(Multipart g)
        {
            JArray jPolygon = new JArray();

            foreach (var part in g.Parts)
            {
                JArray jPart = new JArray();
                jPolygon.Add(jPart);
                JArray jPoint;
                foreach (var point in part.Points)
                {
                    jPoint = new JArray();
                    jPart.Add(jPoint);
                    jPoint.Add(point.X);
                    jPoint.Add(point.Y);
                }
                if (g is Polygon)
                {
                    jPoint = new JArray();
                    jPart.Add(jPoint);
                    jPoint.Add(part.StartPoint.X);
                    jPoint.Add(part.StartPoint.Y);
                }
            }
            return jPolygon;
        }

        /// <summary>
        /// 将要素中的属性转为Json
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private static JObject GetPropertiesJson(Feature feature)
        {
            JObject jProps = new JObject();
            foreach (var prop in feature.Attributes)
            {
                if (prop.Value == null)
                {
                    jProps.Add(prop.Key, null);
                    continue;
                }
                switch (prop.Value)
                {
                    case string s:
                        jProps.Add(prop.Key, s);
                        break;

                    case int i32:
                        jProps.Add(prop.Key, i32);
                        break;

                    case long i64:
                        jProps.Add(prop.Key, i64);
                        break;

                    case float f:
                        jProps.Add(prop.Key, f);
                        break;

                    case double d:
                        jProps.Add(prop.Key, d);
                        break;

                    case DateTime dt:
                        jProps.Add(prop.Key, dt.ToString(Parameters.DateFormat));
                        break;

                    case DateTimeOffset dto:
                        jProps.Add(prop.Key, dto.UtcDateTime.ToString(Parameters.DateFormat));
                        break;

                    default:
                        jProps.Add(prop.Key, prop.Value.ToString());
                        break;
                }
            }
            return jProps;
        }

        class GeoJsonStyleConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                Debug.WriteLine(objectType.FullName);
                if (objectType == typeof(System.Drawing.Color))
                {
                    return true;
                }
                return false;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value is System.Drawing.Color color)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(color.A);
                    writer.WriteValue(color.R);
                    writer.WriteValue(color.G);
                    writer.WriteValue(color.B);
                    writer.WriteEndArray();
                }
                else
                {
                    writer.WriteValue(Array.Empty<int>());
                }
            }
        }
    }
}