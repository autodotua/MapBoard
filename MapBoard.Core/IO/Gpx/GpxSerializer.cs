using Esri.ArcGISRuntime.Geometry;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapBoard.IO.Gpx
{
    public static class GpxSerializer
    {
        private static readonly Dictionary<string, Action<Gpx, string>> xml2GpxProperty = new Dictionary<string, Action<Gpx, string>>()
        {
            ["creator"] = (g, v) => g.Creator = v,
            ["version"] = (g, v) => g.Version = v,
            ["name"] = (g, v) => g.Name = v,
            ["author"] = (g, v) => g.Author = v,
            ["url"] = (g, v) => g.Url = v,
            ["distance"] = (g, v) => { if (double.TryParse(v, out double result)) g.Distance = result; },
            ["time"] = (g, v) => { if (DateTime.TryParse(v, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out DateTime time)) g.Time = time; },
            ["keywords"] = (g, v) => g.KeyWords = v,
        };
        private static readonly Dictionary<string, Action<GpxTrack, string>> xml2TrackProperty = new Dictionary<string, Action<GpxTrack, string>>()
        {
            ["name"] = (g, v) => g.Name = v,
            ["desc"] = (g, v) => g.Description = v,
        };

        /// 从文件加载GPX
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<Gpx> FromFileAsync(string path)
        {
            Gpx gpx = null;
            await Task.Run(() =>
            {
                var xmlString = File.ReadAllText(path);
                gpx = LoadFromString(xmlString, path);
            });
            return gpx;
        }

        /// <summary>
        /// 从文件和读取后的内容加载GPX
        /// </summary>
        /// <param name="xmlString"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="XmlException"></exception>
        public static Gpx LoadFromString(string xmlString, string path)
        {
            Gpx gpx = new Gpx();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            XmlElement gpxNode = xmlDoc["gpx"] ?? throw new XmlException("没有找到gpx元素");
            gpx.FilePath = path;
            foreach (XmlAttribute attribute in gpxNode.Attributes)
            {
                SetGpxValue(gpx, attribute.Name, attribute.Value);
            }
            //老版本的GPX类错误地将metadata中的元数据放在了gpx节点下
            foreach (XmlElement element in gpxNode.ChildNodes)
            {
                SetGpxValue(gpx, element.Name, element.InnerText);
            }
            //读取元数据
            if (gpxNode["metadata"] != null)
            {
                var metadataNode = gpxNode["metadata"];
                foreach (XmlElement element in metadataNode.ChildNodes)
                {
                    SetGpxValue(gpx, element.Name, element.InnerText);
                }
                //扩展元数据
                if (metadataNode["extensions"] != null)
                {
                    var extensionNode = metadataNode["extensions"];
                    foreach (XmlElement extensionElement in extensionNode.ChildNodes)
                    {
                        //单独处理Distance
                        if (extensionElement.Name == "distance")
                        {
                            SetGpxValue(gpx, "distance", extensionElement.InnerText);
                        }
                        else
                        {
                            gpx.Extensions.Add(extensionElement.Name, extensionElement.InnerText);
                        }
                    }
                }
            }
            if (gpxNode["trk"] != null)
            {
                LoadTrack(gpx.CreateTrack(), gpxNode["trk"]);
            }
            return gpx;
        }

        public static Gpx LoadMetadatasFromFile(string file)
        {
            throw new NotImplementedException();
            //using XmlReader xr = XmlReader.Create(file);
            //xr.MoveToContent();
            //if (xr.NodeType == XmlNodeType.Element)
            //{
            //    while (xr.MoveToNextAttribute())
            //    {
            //        switch (xr.Name)
            //        {
            //            case "creator":
            //                Creator = xr.Value;
            //                break;

            //            case "version":
            //                Version = xr.Value;
            //                break;
            //        }
            //    }
            //    while (xr.Read())
            //    {
            //        xr.MoveToContent();
            //        string xrName = xr.Name;
            //        xr.Read();
            //        string xrValue = xr.Value;
            //        switch (xrName)
            //        {
            //            case "name":
            //                Name = xrValue;
            //                break;

            //            case "author":
            //                Author = xrValue;
            //                break;

            //            case "url":
            //                Url = xrValue;
            //                break;


            //            case "distance":
            //                if (double.TryParse(xrValue, out double result))
            //                {
            //                    Distance = result;
            //                }
            //                break;

            //            case "time":
            //                if (DateTime.TryParse(xrValue, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out DateTime time))
            //                {
            //                    Time = time;
            //                }
            //                break;

            //            case "keywords":
            //                KeyWords = xrValue;
            //                break;

            //        }
            //        xr.Read();//End Element
            //    }
            //}
        }

        public static async Task<IList<Gpx>> MetadatasFromFilesAsync(IList<string> files)
        {
            Gpx[] gpxs = new Gpx[files.Count];
            await Task.Run(() =>
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    gpxs[i] = LoadMetadatasFromFile(file);
                }
            });
            return gpxs;
        }
        /// <summary>
        /// 保存到原始位置
        /// </summary>
        /// <param name="path"></param>
        public static void Save(this Gpx gpx, string path)
        {
            File.WriteAllText(path, gpx.ToXmlString());
        }

        /// <summary>
        /// 获取GPX的XML字符串
        /// </summary>
        /// <returns></returns>
        public static string ToXmlString(this Gpx gpx)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", "no");
            doc.AppendChild(dec);

            XmlElement root = doc.CreateElement("gpx");
            doc.AppendChild(root);
            XmlElement metadata = doc.CreateElement("metadata");
            root.AppendChild(metadata);

            root.SetAttribute("version", gpx.Version);
            root.SetAttribute("creator", gpx.Creator);
            metadata.AppendElement("name", gpx.Name);
            metadata.AppendElement("author", gpx.Author);
            metadata.AppendElement("url", gpx.Url);
            metadata.AppendElement("time", gpx.Time.ToString(Gpx.GpxTimeFormat));
            metadata.AppendElement("keywords", gpx.KeyWords);
            metadata.AppendElement("distance", Math.Round(gpx.Tracks.Sum(p => p.GetPoints().GetDistance()), 2).ToString());
            var metadataExtensions = doc.CreateElement("extensions");
            metadata.AppendChild(metadataExtensions);
            foreach (var item in gpx.Extensions)
            {
                metadataExtensions.AppendElement(item.Key, item.Value);
            }
            foreach (var trk in gpx.Tracks)
            {
                XmlElement node = doc.CreateElement("trk");
                trk.WriteTrackXml(node);
                root.AppendChild(node);
            }

            using var stringWriter = new StringWriter();
            XmlWriterSettings xmlSettingsWithIndentation = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
            };
            using var xmlTextWriter = XmlWriter.Create(stringWriter, xmlSettingsWithIndentation);
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            return stringWriter.GetStringBuilder().ToString();
        }

        private static void AppendElement(this XmlElement parent, string name, string value)
        {
            XmlElement child = parent.OwnerDocument.CreateElement(name);
            child.InnerText = value;
            parent.AppendChild(child);
        }

        private static void LoadSegment(GpxSegment seg, XmlElement segmentNode)
        {
            foreach (XmlElement pointElement in segmentNode.GetElementsByTagName("trkpt"))
            {
                if (pointElement.Attributes["lon"] == null || pointElement.Attributes["lat"] == null)
                {
                    throw new FormatException("缺少必要属性lon或lat");
                }
                double x = double.Parse(pointElement.Attributes["lon"].Value);
                double y = double.Parse(pointElement.Attributes["lat"].Value);
                DateTime? time = pointElement["time"] == null ? null : DateTime.ParseExact(pointElement["time"].InnerText, Gpx.GpxTimeFormat, CultureInfo.InvariantCulture);
                double? z = pointElement["ele"] == null ? null : double.Parse(pointElement["ele"].InnerText);
                GpxPoint point = new GpxPoint(x, y, z, time);
                seg.Points.Add(point);
            }
        }

        private static void LoadTrack(GpxTrack track, XmlElement trackNode)
        {
            foreach (XmlElement element in trackNode.Attributes)
            {
                SetTrackValue(track, element.Name, element.Value);
            }
            foreach (XmlElement segmentElement in trackNode.GetElementsByTagName("trkseg"))
            {
                LoadSegment(track.CreateSegment(), segmentElement);
            }
        }
        private static void SetGpxValue(Gpx gpx, string key, string value)
        {
            if (xml2GpxProperty.TryGetValue(key, out Action<Gpx, string> func))
            {
                func(gpx, value);
            }
        }

        private static void SetTrackValue(GpxTrack track, string key, string value)
        {
            if (xml2TrackProperty.TryGetValue(key, out Action<GpxTrack, string> func))
            {
                func(track, value);
            }
        }

        private static void WriteSegmentXml(this GpxSegment seg, XmlElement segElement)
        {
            foreach (var point in seg.Points)
            {
                var pointElement = segElement.OwnerDocument.CreateElement("wpt");
                segElement.AppendChild(pointElement);
                segElement.SetAttribute("lon", point.X.ToString());
                segElement.SetAttribute("lat", point.Y.ToString());
                if (point.Z.HasValue)
                {
                    segElement.AppendElement("ele", point.Extensions.ToString());
                }
                if (point.Time.HasValue)
                {
                    segElement.AppendElement("time", point.Time.ToString());
                }
            }
        }
        private static void WriteTrackXml(this GpxTrack track, XmlElement trackElement)
        {
            trackElement.AppendElement("name", track.Name);
            trackElement.AppendElement("desc", track.Description);
            foreach (var extension in track.Extensions)
            {
                trackElement.AppendElement(extension.Key, extension.Value);
            }
            foreach (var seg in track.Segments)
            {
                var segElement = trackElement.OwnerDocument.CreateElement("trkseg");
                trackElement.AppendChild(segElement);
                WriteSegmentXml(seg, segElement);
            }

        }
    }
}