using EGIS.ShapeFileLib;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using System.Text.RegularExpressions;
using FzLib;
using FzLib.DataAnalysis;
using System.Diagnostics;

namespace MapBoard.IO
{
    public static class MobileGeodatabase
    {
        public const string MgdbFileName = "layers.geodatabase";
        public readonly static string MgdbFilePath = Path.Combine(FolderPaths.DataPath, MgdbFileName);

        public static Geodatabase Current { get; private set; }

        public static async Task InitializeAsync()
        {
            if (Current != null)
            {
                return;
                //throw new InvalidOperationException("已经初始化，无法再次初始化");
            }
            if (!Directory.Exists(FolderPaths.DataPath))
            {
                Directory.CreateDirectory(FolderPaths.DataPath);
            }
            var gdbFile = Path.Combine(FolderPaths.DataPath, MgdbFileName);
            if (!File.Exists(gdbFile))
            {
                Current = await Geodatabase.CreateAsync(gdbFile);
            }
            else
            {
                Current = await Geodatabase.OpenAsync(gdbFile);
            }
        }

        public static async Task ClearAsync()
        {
            foreach (var table in Current.GeodatabaseFeatureTables.ToList())
            {
                await Current.DeleteTableAsync(table.TableName);
            }
        }

        public static async Task ImportFeatureTableAsync(string name, FeatureTable featureTable, IEnumerable<FieldInfo> oldFields = null)
        {
            if (oldFields == null)
            {
                TableDescription tableDescription = new TableDescription(name, featureTable.SpatialReference, featureTable.GeometryType);
                foreach (var field in featureTable.Fields)
                {
                    tableDescription.FieldDescriptions.Add(new FieldDescription(field.Name, field.FieldType));
                }
                var newTable = await Current.CreateTableAsync(tableDescription);
                await newTable.AddFeaturesAsync(await featureTable.QueryFeaturesAsync(new QueryParameters()));
            }
            else
            {
                //在旧版本中，使用Shapefile存储空间数据。
                //由于Shapefile不支持时间类型，所以使用的Text来存储时间。
                //在新版本中，需要将字符串的时间类型转换为真正的时间类型。
                TableDescription tableDescription = new TableDescription(name, featureTable.SpatialReference, featureTable.GeometryType);
                HashSet<string> timeKeys = new HashSet<string>();
                HashSet<string> dateKeys = new HashSet<string>();
                HashSet<string> oldFieldKeys = featureTable.Fields.Select(p => p.Name).ToHashSet();
                foreach (var field in oldFields)
                {
                    if (!oldFieldKeys.Contains(field.Name))
                    {
                        throw new Exception($"字段{field.Name}出现在旧的字段定义中，但在提供的要素类中不存在");
                    }
                    if (field.IsIdField())
                    {
                        continue;
                    }
                    tableDescription.FieldDescriptions.Add(field.ToFieldDescription());
                    if (field.Type == FieldInfoType.DateTime)
                    {
                        timeKeys.Add(field.Name);
                    }
                    if (field.Type == FieldInfoType.Date)
                    {
                        dateKeys.Add(field.Name);
                    }
                }

                var newTable = await Current.CreateTableAsync(tableDescription);
                if (timeKeys.Count == 0)
                {
                    //没有时间字段，直接一次性加进去
                    await newTable.AddFeaturesAsync(await featureTable.QueryFeaturesAsync(new QueryParameters()));
                }
                else
                {
                    var oldFeatures = (await featureTable.QueryFeaturesAsync(new QueryParameters())).ToList();
                    List<Feature> features = new List<Feature>(oldFeatures.Count);
                    foreach (var feature in oldFeatures)
                    {
                        //删除ID字段
                        foreach (var attr in feature.Attributes.ToList())
                        {
                            if(FieldExtension.IsIdField(attr.Key))
                            {
                                feature.Attributes.Remove(attr.Key);
                            }
                        }
                        //string的time转DateTime
                        foreach (var timeKey in timeKeys)
                        {
                            var value = feature.GetAttributeValue(timeKey);
                            if (value is not string str)//不是字符串
                            {
                                Debug.WriteLine($"旧的时间字段{timeKey}不是字符串：{value}");
                                Debug.Assert(false);
                                continue;
                            }

                            if (string.IsNullOrEmpty(str))//字符串为空
                            {
                                feature.SetAttributeValue(timeKey, null);
                                continue;
                            }

                            if (!DateTime.TryParse(str, out var timeValue))//无法转为时间
                            {
                                Debug.WriteLine($"时间字段{timeKey}无法转为时间：{str}");
                                Debug.Assert(false);
                                continue;
                            }

                            feature.SetAttributeValue(timeKey, timeValue);
                        }
                        //DateTime的Date转DateOnly
                        foreach (var dateKey in dateKeys)
                        {
                            var value = feature.GetAttributeValue(dateKey);
                            if(value==null)
                            {
                                continue;
                            }
                            if (value is not DateTime dt)//不是DateTimeOffset
                            {
                                Debug.WriteLine($"旧的时间字段{dateKey}不是{nameof(DateTime)}：{value}");
                                Debug.Assert(false);
                                continue;
                            }

                            feature.SetAttributeValue(dateKey, DateOnly.FromDateTime(dt));
                        }
                        features.Add(feature);
                    }
                    await newTable.AddFeaturesAsync(features);
                }
            }
        }

        public static async Task<GeodatabaseFeatureTable> CreateMgdbLayerAsync(GeometryType type, string name, string folder = null, IEnumerable<FieldInfo> fields = null)
        {
            //排除由ArcGIS自动创建的临时字段，判断字段合法性
            fields = fields
                .Where(p => !p.IsIdField())//ID
                .Where(p => p.Name.ToLower() != "shape_leng")//长度
                .Where(p => p.Name.ToLower() != "shape_area");//面积
            if (fields.Any(field => string.IsNullOrEmpty(field.Name)
            || !Regex.IsMatch(field.Name[0].ToString(), "[a-zA-Z]")
                  || !Regex.IsMatch(field.Name, "^[a-zA-Z0-9_]+$")))
            {
                throw new ArgumentException($"存在不合法的字段名");
            }

            TableDescription td = new TableDescription(name, SpatialReferences.Wgs84, type);
            foreach (var field in fields)
            {
                td.FieldDescriptions.Add(field.ToFieldDescription());
            }
            var table = await Current.CreateTableAsync(td);
            await table.LoadAsync();
            return table;
        }

        public static Task CopyToDirAsync(string directory)
        {
            return Task.Run(() =>
            {
                File.Copy(MgdbFilePath, Path.Combine(directory, MgdbFileName));
            });
        }

        public static async Task ReplaceFromMGDBAsync(string mgdbPath)
        {
            Current.Close();
            Current = null;
            await Task.Run(() =>
            {
                File.Copy(mgdbPath, MgdbFilePath, true);
            });
            await InitializeAsync();
        }
    }
}