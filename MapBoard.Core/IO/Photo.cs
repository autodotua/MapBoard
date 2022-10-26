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
using ImageMagick;
using Rational = MetadataExtractor.Rational;

namespace MapBoard.IO
{
    public static class Photo
    {
        public static readonly string NameField = "name";
        public static readonly string ImagePathField = "ImagePath";
        public static readonly string DateField = "Date";
        public static readonly string TimeField = "Time";

        public static async Task ImportImageLocation(IEnumerable<string> files, MapLayerCollection layers)
        {
            var fields = new[]
            {
                new FieldInfo(NameField,"名称",FieldInfoType.Text),
                new FieldInfo(ImagePathField,"路径",FieldInfoType.Text),
                new FieldInfo(DateField,"拍摄日期",FieldInfoType.Date),
                new FieldInfo(TimeField,"拍摄时间",FieldInfoType.Time),
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
                                    attr.Add(DateField, info.Value.time.Value);
                                    attr.Add(TimeField, info.Value.time.Value.ToString(Parameters.TimeFormat));
                                }
                                attr.Add(NameField, Path.GetFileName(file));
                                attr.Add(ImagePathField, file);
                                var feature = layer.CreateFeature(attr, point);
                                features.Add(feature);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"照片{file}解析失败：" + ex.Message);
                        }
                    });
            });
            if (features.IsEmpty)
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

        public static async Task<string> GetDisplayableImage(string imagePath,int maxLength)
        {
            string tempPath = Path.GetTempFileName() + ".jpg";
            await Task.Run(() =>
            {
                using MagickImage image = new MagickImage(imagePath);
                image.AdaptiveResize(maxLength, maxLength);
                image.Write(tempPath);
            });
            return tempPath;
        }
    }
}