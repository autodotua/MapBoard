﻿using Esri.ArcGISRuntime.Geometry;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapBoard.IO.Gpx
{
    public static class GpxSerializer
    {
        public const string GpxTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        #region 文件读写

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
        /// 保存到原始位置
        /// </summary>
        /// <param name="path"></param>
        public static void Save(this Gpx gpx, string path)
        {
            File.WriteAllText(path, gpx.ToXmlString());
        }
        #endregion

        #region Metadata

        public static Gpx LoadMetadatasFromFile(string file)
        {
            //XmlReader有点难用，让ChatGPT帮我写了。
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                throw new ArgumentException("Invalid file path or file not found.");
            }

            Gpx gpx = new Gpx();

            using (XmlReader reader = XmlReader.Create(file))
            {
                reader.ReadStartElement("gpx");

                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "version")
                    {
                        gpx.Version = reader.Value;
                    }
                    else if (reader.Name == "creator")
                    {
                        gpx.Creator = reader.Value;
                    }
                }

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "metadata":
                                ReadMetadata(reader, gpx);
                                break;
                        }
                    }
                }
            }

            return gpx;
        }

        private static void ReadMetadata(XmlReader reader, Gpx gpx)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            gpx.Name = reader.ReadElementContentAsString();
                            break;
                        case "desc":
                            gpx.Description = reader.ReadElementContentAsString();
                            break;
                        case "author":
                            gpx.Author = reader.ReadElementContentAsString();
                            break;
                        case "time":
                            gpx.Time = DateTime.Parse(reader.ReadElementContentAsString());
                            break;
                        case "keywords":
                            gpx.KeyWords = reader.ReadElementContentAsString();
                            break;
                        case "extensions":
                            ReadExtensions(reader, gpx);
                            break;
                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "metadata")
                {
                    break;
                }
            }
        }

        private static void ReadExtensions(XmlReader reader, Gpx gpx)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "distance")
                {
                    gpx.Distance = double.Parse(reader.ReadElementContentAsString());
                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "extensions")
                {
                    break;
                }
            }
        }

        public static async Task<IList<Gpx>> LoadMetadatasFromFiles(IList<string> files)
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

        #endregion

        #region XML => Object

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
                DateTime? time = null;
                double? z = null;
                Dictionary<string, string> extensions = new Dictionary<string, string>();
                foreach (XmlElement child in pointElement.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "time":
                            time = DateTime.ParseExact(child.InnerText, GpxTimeFormat, CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal);
                            break;
                        case "ele":
                            z = double.Parse(child.InnerText);
                            break;
                        default:
                            extensions.Add(child.Name, child.InnerText);
                            break;
                    }
                }
                GpxPoint point = new GpxPoint(x, y, z, time);
                point.Extensions = extensions;
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
        #endregion

        #region Object => XML

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
            metadata.AppendElement("time", gpx.Time.ToString(GpxTimeFormat));
            metadata.AppendElement("keywords", gpx.KeyWords);


            var metadataExtensions = doc.CreateElement("extensions");
            metadata.AppendChild(metadataExtensions); metadataExtensions.AppendElement("distance", Math.Round(gpx.Tracks.Sum(p => p.GetPoints().GetDistance()), 2).ToString());
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

        private static void AppendExtensions(IGpxElement gpxObj, XmlElement parentElement)
        {
            if (gpxObj.Extensions != null || gpxObj.Extensions.Count > 0)
            {
                XmlElement extensionElement = null;
                foreach (var extension in gpxObj.Extensions)
                {
                    if (gpxObj.HiddenElements.Contains(extension.Key))
                    {
                        parentElement.AppendElement(extension.Key, extension.Value);
                    }
                    else
                    {
                        if (extensionElement == null)
                        {
                            extensionElement = parentElement.OwnerDocument.CreateElement("extensions");
                            parentElement.AppendChild(extensionElement);
                        }
                        extensionElement.AppendElement(extension.Key, extension.Value);
                    }
                }
            }
        }

        private static void WriteSegmentXml(this GpxSegment seg, XmlElement segElement)
        {
            foreach (var point in seg.Points)
            {
                var pointElement = segElement.OwnerDocument.CreateElement("trkpt");
                segElement.AppendChild(pointElement);
                pointElement.SetAttribute("lon", point.X.ToString());
                pointElement.SetAttribute("lat", point.Y.ToString());
                if (point.Z.HasValue)
                {
                    pointElement.AppendElement("ele", point.Z.ToString());
                }
                if (point.Time.HasValue)
                {
                    pointElement.AppendElement("time", point.Time.Value.ToString(GpxTimeFormat));
                }
                AppendExtensions(point, pointElement);
            }
            AppendExtensions(seg, segElement);
        }

        private static void WriteTrackXml(this GpxTrack track, XmlElement trackElement)
        {
            trackElement.AppendElement("name", track.Name);
            trackElement.AppendElement("desc", track.Description);

            AppendExtensions(track, trackElement);
            foreach (var seg in track.Segments)
            {
                var segElement = trackElement.OwnerDocument.CreateElement("trkseg");
                trackElement.AppendChild(segElement);
                WriteSegmentXml(seg, segElement);
            }

        }
        #endregion
    }
}