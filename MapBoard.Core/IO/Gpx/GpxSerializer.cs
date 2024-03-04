using Esri.ArcGISRuntime.Geometry;
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

        public static async Task<IList<Gpx>> MetadatasFromFilesAsync(IList<string> files)
        {
            Gpx[] gpxs = new Gpx[files.Count];
            await Task.Run(() =>
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    gpxs[i] = new Gpx();
                    gpxs[i].LoadMetadatasFromFile(file);
                }
            });
            return gpxs;
        }

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
                gpx= LoadFromString(xmlString, path);
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
                SetGpxValue(gpx,attribute.Name, attribute.Value);
            }
            //老版本的GPX类错误地将metadata中的元数据放在了gpx节点下
            foreach (XmlElement element in gpxNode.ChildNodes)
            {
                SetGpxValue(gpx, element.Name, element.Value);
            }
            //读取元数据
            if (gpxNode["metadata"] != null)
            {
                var metadataNode = gpxNode["metadata"];
                foreach (XmlElement element in metadataNode.ChildNodes)
                {
                    SetGpxValue(gpx,element.Name, element.Value);
                }
                //扩展元数据
                if (metadataNode["extensions"]!=null)
                {
                    var extensionNode = metadataNode["extensions"];
                    foreach (XmlElement extensionElement in extensionNode.ChildNodes)
                    {
                        //单独处理Distance
                        if(extensionElement.Name== "distance")
                        {
                            SetGpxValue(gpx,"distance", extensionElement.Value);
                        }
                        else
                        {
                            gpx.Extensions.Add(extensionElement.Name, extensionElement.Value);
                        }
                    }
                }
            }
            if (gpxNode["trk"] != null)
            {
                GpxTrack track = gpx.CreateTrack();

                throw new NotImplementedException();
                //GpxTrack.LoadGpxTrackInfoProperties()
            }
            return gpx;
        }

        private void LoadTrack(GpxTrack track,XmlElement trackNode,XmlDocument rootNode)
        {
            foreach (XmlElement element in trackNode.ChildNodes)
            {
                SetTrackValue(track, element.Name, element.Value);
            }
            foreach (XmlElement segmentElement in trackNode.GetElementsByTagName("trkseg"))
            {

            }
        }


        public void LoadMetadatasFromFile(string file)
        {
            throw new NotImplementedException();
            using XmlReader xr = XmlReader.Create(file);
            xr.MoveToContent();
            if (xr.NodeType == XmlNodeType.Element)
            {
                while (xr.MoveToNextAttribute())
                {
                    switch (xr.Name)
                    {
                        case "creator":
                            Creator = xr.Value;
                            break;

                        case "version":
                            Version = xr.Value;
                            break;
                    }
                }
                while (xr.Read())
                {
                    xr.MoveToContent();
                    string xrName = xr.Name;
                    xr.Read();
                    string xrValue = xr.Value;
                    switch (xrName)
                    {
                        case "name":
                            Name = xrValue;
                            break;

                        case "author":
                            Author = xrValue;
                            break;

                        case "url":
                            Url = xrValue;
                            break;


                        case "distance":
                            if (double.TryParse(xrValue, out double result))
                            {
                                Distance = result;
                            }
                            break;

                        case "time":
                            if (DateTime.TryParse(xrValue, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out DateTime time))
                            {
                                Time = time;
                            }
                            break;

                        case "keywords":
                            KeyWords = xrValue;
                            break;

                    }
                    xr.Read();//End Element
                }
            }
        }

        /// <summary>
        /// 保存到原始位置
        /// </summary>
        /// <param name="path"></param>
        public static void Save(this Gpx gpx,string path)
        {
            File.WriteAllText(path, gpx.ToXml());
        }

        /// <summary>
        /// 获取GPX的XML字符串
        /// </summary>
        /// <returns></returns>
        public static string ToXml(this Gpx gpx)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", "no");
            doc.AppendChild(dec);

            XmlElement root = doc.CreateElement("gpx");
            doc.AppendChild(root);
            XmlElement metadata = doc.CreateElement("metadata");
            root.AppendChild(metadata);

            root.SetAttribute("version", gpx. Version);
            root.SetAttribute("creator", gpx.Creator);
            AppendToMetadata("name", gpx.Name);
            AppendToMetadata("author", gpx.Author);
            AppendToMetadata("url", gpx.Url);
            AppendToMetadata("time", gpx.Time.ToString(Gpx.GpxTimeFormat));
            AppendToMetadata("keywords", gpx.KeyWords);
            AppendToMetadata("distance", Math.Round(gpx.Tracks.Sum(p => p.Distance), 2).ToString());
            foreach (var item in gpx.Extensions)
            {
                if (item.Key.Contains("xmlns"))
                {
                }
                else if (item.Key.Contains(':'))
                {
                }
                else
                {
                    AppendToMetadata(item.Key, item.Value);
                }
            }
            foreach (var trk in gpx.Tracks)
            {
                XmlElement node = doc.CreateElement("trk");
                trk.WriteGpxXml(doc, node);
                root.AppendChild(node);
            }

            return GetXmlString();

            void AppendToMetadata(string name, string value)
            {
                XmlElement child = doc.CreateElement(name);
                child.InnerText = value;
                metadata.AppendChild(child);
            }
            string GetXmlString()
            {
                string xmlString = null;
                using (var stringWriter = new StringWriter())
                {
                    XmlWriterSettings xmlSettingsWithIndentation = new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "\t",
                    };
                    using var xmlTextWriter = XmlWriter.Create(stringWriter, xmlSettingsWithIndentation);
                    doc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    xmlString = stringWriter.GetStringBuilder().ToString();
                }

                return xmlString;
            }
        }
        private static void SetGpxValue(Gpx gpx,string key, string value)
        {
            if (xml2GpxProperty.TryGetValue(key, out Action<Gpx, string> func))
            {
                func(gpx, value);
            }
        }
        private static void SetTrackValue(GpxTrack track,string key, string value)
        {
            if (xml2TrackProperty.TryGetValue(key, out Action<GpxTrack, string> func))
            {
                func(track, value);
            }
        }
    }
}