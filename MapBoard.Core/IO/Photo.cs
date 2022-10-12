using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapBoard.Mapping.Model;
using MetadataExtractor;
using System.Diagnostics;

namespace MapBoard.IO
{
    public static class Photo
    {
        public static async Task ImportImageLocation(IEnumerable<string> files, MapLayerCollection layers)
        {
            string nameField = "name";
            string addressField = "address";
            string dateField = "date";
            string timeField = "time";
            var fields = new[]
            {
                      new FieldInfo(nameField,"名称",FieldInfoType.Text),
                      new FieldInfo(addressField,"路径",FieldInfoType.Text),
                      new FieldInfo(dateField,"拍摄日期",FieldInfoType.Date),
                      new FieldInfo(timeField,"拍摄时间",FieldInfoType.Time),
            };
            var layer = await LayerUtility.CreateShapefileLayerAsync(GeometryType.Point, layers, fields: fields);
            ConcurrentBag<Feature> features = new ConcurrentBag<Feature>();
            await Task.Run(() =>
            {
                Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 4, }, file =>
                    {
                        try
                        {
                            var info = GetImageExifInfo(file);
                            if (info != null)
                            {
                                MapPoint point = new MapPoint(info.Value.lng, info.Value.lat, SpatialReferences.Wgs84);
                                Dictionary<string, object> attr = new Dictionary<string, object>();
                                if (info.Value.time.HasValue)
                                {
                                    attr.Add(dateField, info.Value.time.Value);
                                    attr.Add(timeField, info.Value.time.Value.ToString(Parameters.TimeFormat));
                                }
                                attr.Add(nameField, Path.GetFileName(file));
                                attr.Add(addressField, file);
                                var feature = layer.CreateFeature(attr, point);
                                features.Add(feature);
                            }
                        }
                        catch(Exception ex)
                        {
                            Debug.WriteLine($"照片{file}解析失败：" + ex.Message);
                        }
                    });
            });
            if(features.IsEmpty)
            {
                throw new Exception("指定的目录中不存在包含坐标信息的图片");
            }
            await layer.AddFeaturesAsync(features, FeaturesChangedSource.Import);
        }

        private static double GetDegree(Rational[] rationals)
        {
            if (rationals == null || rationals.Length != 3)
            {
                throw new ArgumentException();
            }

            double d = rationals[0].Numerator;
            double m = rationals[1].Numerator;
            double s = 1.0 * rationals[2].Numerator / rationals[2].Denominator;
            return d + m / 60 + s / 3600;
        }

        private static (double lng, double lat, DateTime? time)? GetImageExifInfo(string path)
        {
            double latDegree = double.NaN;
            double lngDegree = double.NaN;

            DateTime? time = null;
            var directories = ImageMetadataReader.ReadMetadata(path).ToList();
            var gps = directories.FirstOrDefault(p => p.Name == "GPS");
            if (gps != null && !gps.HasError)
            {
                var lat = gps.GetRationalArray(2);
                var lng = gps.GetRationalArray(4);
                if (lat != null && lng != null)
                {
                    latDegree = GetDegree(lat);
                    lngDegree = GetDegree(lng);

                    var main = directories.FirstOrDefault(p => p.Name == "Exif IFD0");
                    if (main != null && !main.HasError)
                    {
                        time = main.GetDateTime(306);
                    }
                }
            }

            if (!double.IsNaN(latDegree) && !double.IsNaN(lngDegree))
            {
                return (lngDegree, latDegree, time);
            }
            return null;
        }
    }
}