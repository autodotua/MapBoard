using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using MapBoard.Common;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
{
    public static class Photo
    {
        private static (double lng, double lat, DateTime? time)? GetImageExifInfo(string jpgPath)
        {
            double lat = double.NaN;
            double lng = double.NaN;
            DateTime? date = null;

            using (var image = System.Drawing.Image.FromFile(jpgPath))
            {
                var propertyItems = image.PropertyItems.OrderBy(x => x.Id)
                .Where(p => p.Id >= 0x0000 && p.Id <= 0x001e || p.Id == 0x9003);
                foreach (var objItem in propertyItems)
                {
                    switch (objItem.Id)
                    {
                        case 0x0002://设置纬度
                            if (objItem.Value.Length == 24)
                            {
                                //degrees(将byte[0]~byte[3]转成uint, 除以byte[4]~byte[7]转成的uint)
                                double d = BitConverter.ToUInt32(objItem.Value, 0) * 1.0d / BitConverter.ToUInt32(objItem.Value, 4);
                                //minutes(將byte[8]~byte[11]转成uint, 除以byte[12]~byte[15]转成的uint)
                                double m = BitConverter.ToUInt32(objItem.Value, 8) * 1.0d / BitConverter.ToUInt32(objItem.Value, 12);
                                //seconds(將byte[16]~byte[19]转成uint, 除以byte[20]~byte[23]转成的uint)
                                double s = BitConverter.ToUInt32(objItem.Value, 16) * 1.0d / BitConverter.ToUInt32(objItem.Value, 20);
                                lat = (((s / 60 + m) / 60) + d);
                            }
                            break;

                        case 0x0004: //设置经度
                            if (objItem.Value.Length == 24)
                            {
                                //degrees(将byte[0]~byte[3]转成uint, 除以byte[4]~byte[7]转成的uint)
                                double d = BitConverter.ToUInt32(objItem.Value, 0) * 1.0d / BitConverter.ToUInt32(objItem.Value, 4);
                                //minutes(将byte[8]~byte[11]转成uint, 除以byte[12]~byte[15]转成的uint)
                                double m = BitConverter.ToUInt32(objItem.Value, 8) * 1.0d / BitConverter.ToUInt32(objItem.Value, 12);
                                //seconds(将byte[16]~byte[19]转成uint, 除以byte[20]~byte[23]转成的uint)
                                double s = BitConverter.ToUInt32(objItem.Value, 16) * 1.0d / BitConverter.ToUInt32(objItem.Value, 20);
                                lng = (((s / 60 + m) / 60) + d);
                            }
                            break;

                        case 0x9003:
                            var propItemValue = objItem.Value;
                            var dateTimeStr = System.Text.Encoding.ASCII.GetString(propItemValue).Trim('\0');
                            date = DateTime.ParseExact(dateTimeStr, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
                            break;
                    }
                }
            }
            GC.Collect();
            return double.IsNaN(lng) || double.IsNaN(lat) ? null : (lng, lat, date);
        }

        public static async Task ImportImageLocation(IEnumerable<string> files, MapLayerCollection layers)
        {
            string addressField = "Addr";
            var fields = new[]
            {
                      new FieldInfo(addressField,"照片位置",FieldInfoType.Text),
            };
            var layer = await LayerUtility.CreateLayerAsync(GeometryType.Point, layers, fields: fields);
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
                                    attr.Add(Resource.DateFieldName, info.Value.time);
                                }
                                attr.Add(Resource.LabelFieldName, Path.GetFileName(file));
                                attr.Add(addressField, file);
                                var feature = layer.CreateFeature(attr, point);
                                features.Add(feature);
                            }
                        }
                        catch
                        {
                        }
                    });
            });
            await layer.AddFeaturesAsync(features);
        }
    }
}