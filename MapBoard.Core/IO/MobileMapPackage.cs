using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Linq;
using Newtonsoft.Json.Linq;
using MapBoard.Util;
using System.Net;
using System.Threading;
using Esri.ArcGISRuntime.Data;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;
namespace MapBoard.IO
{
    public class MobileMapPackage
    {
        /// <summary>
        /// 导入shapefile文件到新图层
        /// </summary>
        /// <param name="path"></param>
        public static async Task ImportAsync(string path, MapLayerCollection layers)
        {
            Esri.ArcGISRuntime.Mapping.MobileMapPackage mmpk = await Esri.ArcGISRuntime.Mapping.MobileMapPackage.OpenAsync(path);
            var mmpkLayers = new List<FeatureLayer>();
            foreach (var map in mmpk.Maps)
            {
                mmpkLayers.AddRange(map.OperationalLayers.OfType<FeatureLayer>());
            }
            if (mmpkLayers.Count == 0)
            {
                return;
            }

            foreach (var mmpkLayer in mmpkLayers)
            {
                var rendererJson = mmpkLayer.Renderer.ToJson();
                var labelJsons = mmpkLayer.LabelDefinitions.ToDictionary(p => p.WhereClause, p => p.ToJson());

                var table = mmpkLayer.FeatureTable;

                var layer = await LayerUtility.ImportFromFeatureTable(Path.GetFileNameWithoutExtension(path), layers, table);

                layer.Renderer.RawJson=rendererJson;
                layer.Labels = mmpkLayer.LabelDefinitions.Select(p => new LabelInfo() { RawJson = p.ToJson() }).ToArray();
                layer.ApplyStyle();

                //FeatureQueryResult features = await table.QueryFeaturesAsync(new QueryParameters());
                //var fieldMap = table.Fields.FromEsriFields();//从原表字段名到新字段的映射
                //ShapefileMapLayerInfo layer = await LayerUtility.CreateShapefileLayerAsync(table.GeometryType, layers,
                //     Path.GetFileNameWithoutExtension(path),
                //    fieldMap.Values.ToList());
                //layer.LayerVisible = false;
                //var fields = layer.Fields.Select(p => p.Name).ToHashSet();
                //List<Feature> newFeatures = new List<Feature>();
                //foreach (var feature in features)
                //{
                //    Dictionary<string, object> newAttributes = new Dictionary<string, object>();
                //    foreach (var attr in feature.Attributes)
                //    {
                //        if (attr.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
                //        {
                //            continue;
                //        }
                //        string name = attr.Key;//现在是源文件的字段名

                //        if (!fieldMap.ContainsKey(name))
                //        {
                //            continue;
                //        }
                //        name = fieldMap[name].Name;//切换到目标表的字段名

                //        object value = attr.Value;
                //        if (value is short)
                //        {
                //            value = Convert.ToInt32(value);
                //        }
                //        else if (value is float)
                //        {
                //            value = Convert.ToDouble(value);
                //        }
                //        newAttributes.Add(name, value);
                //    }
                //    Feature newFeature = layer.CreateFeature(newAttributes, feature.Geometry.RemoveZAndM());
                //    newFeatures.Add(newFeature);
                //}
                //await layer.AddFeaturesAsync(newFeatures, FeaturesChangedSource.Import);

                //layer.LayerVisible = true;
            }

        }
    }
}