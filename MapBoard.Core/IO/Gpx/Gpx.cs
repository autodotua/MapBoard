using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MapBoard.IO.Gpx
{
    /// <summary>
    /// GPX数据类型和解析类
    /// </summary>
    public class Gpx : ICloneable
    {
        internal const string GpxTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        public Gpx()
        {
        }

        private Gpx(string path, XmlElement xml, bool metadataOnly)
        {
            LoadGpxInfoProperties(this, xml, metadataOnly);
            FilePath = path;
        }

        public string Author { get; set; }

        public string Creator { get; set; }

        public string Description { get; set; }

        public double Distance { get; set; }

        public string FilePath { get; }

        public string KeyWords { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> OtherProperties { get; private set; } = new Dictionary<string, string>();

        public DateTime Time { get; set; }

        public List<GpxTrack> Tracks { get; private set; } = new List<GpxTrack>();

        public string Url { get; set; }

        public string UrlName { get; set; }

        public string Version { get; set; }

        /// <summary>
        /// 从文件加载GPX
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<Gpx> FromFileAsync(string path)
        {
            var file = await File.ReadAllTextAsync(path);
            Gpx gpx = null;
            await Task.Run(() =>
            {
                gpx = FromString(path, file);
            });
            return gpx;
        }

        public static Gpx MetadataFromFile(string file)
        {
            using XmlReader xr = XmlReader.Create(file);
            xr.MoveToContent();
            Gpx gpx = new Gpx();
            if (xr.NodeType == XmlNodeType.Element)
            {
                while (xr.MoveToNextAttribute())
                {
                    switch (xr.Name)
                    {
                        case "creator":
                            gpx.Creator = xr.Value;
                            break;

                        case "version":
                            gpx.Version = xr.Value;
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
                            gpx.Name = xrValue;
                            break;

                        case "author":
                            gpx.Author = xrValue;
                            break;

                        case "url":
                            gpx.Url = xrValue;
                            break;

                        case "urlname":
                            gpx.UrlName = xrValue;
                            break;

                        case "distance":
                            if (double.TryParse(xrValue, out double result))
                            {
                                gpx.Distance = result;
                            }
                            break;

                        case "time":
                            if (DateTime.TryParse(xrValue, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out DateTime time))
                            {
                                gpx.Time = time;
                            }
                            break;

                        case "keywords":
                            gpx.KeyWords = xrValue;
                            break;

                        case "trk":
                            return gpx;
                    }
                    xr.Read();//End Element
                }
            }
            return gpx;
        }
        public static async Task<IList<Gpx>> MetadatasFromFilesAsync(IList<string> files)
        {
            Gpx[] gpxs = new Gpx[files.Count];
            await Task.Run(() =>
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    gpxs[i] = MetadataFromFile(file);
                }
            });
            return gpxs;
        }



        /// <summary>
        /// 从文件和读取后的内容加载GPX
        /// </summary>
        /// <param name="path"></param>
        /// <param name="gpxString"></param>
        /// <returns></returns>
        /// <exception cref="XmlException"></exception>
        public static Gpx FromString(string path, string gpxString)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(gpxString);
            XmlElement xmlGpx = xmlDoc["gpx"];
            if (xmlGpx == null)
            {
                throw new XmlException("没有找到gpx元素");
            }
            Gpx info = new Gpx(path, xmlGpx, false);

            return info;
        }

        /// <summary>
        /// 加载GPX XML文件的Attributes和ChildNodes
        /// </summary>
        /// <param name="info"></param>
        /// <param name="xml"></param>
        public static void LoadGpxInfoProperties(Gpx info, XmlNode xml, bool metadataOnly)
        {
            LoadGpxInfoProperties(info, xml.Attributes.Cast<XmlAttribute>());
            LoadGpxInfoProperties(info, xml.ChildNodes.Cast<XmlElement>());
        }

        public object Clone()
        {
            var info = MemberwiseClone() as Gpx;
            info.Tracks = Tracks.Select(p => p.Clone() as GpxTrack).ToList();
            return info;
        }

        public GpxTrack CreateTrack()
        {
            var track = new GpxTrack(this);
            Tracks.Add(track);
            return track;
        }
        /// <summary>
        /// 保存到原始位置
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllText(path, ToGpxXml());
        }

        /// <summary>
        /// 获取GPX的XML字符串
        /// </summary>
        /// <returns></returns>
        public string ToGpxXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", "no");
            doc.AppendChild(dec);

            XmlElement root = doc.CreateElement("gpx"); doc.AppendChild(root);

            root.SetAttribute("version", Version);
            root.SetAttribute("creator", Creator);
            AppendChildNode("name", Name);
            AppendChildNode("author", Author);
            AppendChildNode("url", Url);
            AppendChildNode("urlname", UrlName);
            AppendChildNode("time", Time.ToString(GpxTimeFormat));
            AppendChildNode("keywords", KeyWords);
            AppendChildNode("distance", Math.Round(Tracks.Sum(p => p.Distance), 2).ToString());
            foreach (var item in OtherProperties)
            {
                if (item.Key.Contains("xmlns"))
                {
                }
                else if (item.Key.Contains(":"))
                {
                }
                else
                {
                    AppendChildNode(item.Key, item.Value);
                }
            }
            foreach (var trk in Tracks)
            {
                XmlElement node = doc.CreateElement("trk");
                trk.WriteGpxXml(doc, node);
                root.AppendChild(node);
            }

            return GetXmlString();

            void AppendChildNode(string name, string value)
            {
                XmlElement child = doc.CreateElement(name);
                child.InnerText = value;
                root.AppendChild(child);
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

        /// <summary>
        /// 读取节点值，写入到对应的GPX属性
        /// </summary>
        /// <param name="info"></param>
        /// <param name="nodes"></param>
        private static void LoadGpxInfoProperties(Gpx info, IEnumerable<XmlNode> nodes)
        {
            foreach (var node in nodes)
            {
                switch (node.Name)
                {
                    case "creator":
                        info.Creator = node.InnerText;
                        break;

                    case "version":
                        info.Version = node.InnerText;
                        break;

                    case "name":
                        info.Name = node.InnerText;
                        break;

                    case "author":
                        info.Author = node.InnerText;
                        break;

                    case "url":
                        info.Url = node.InnerText;
                        break;

                    case "urlname":
                        info.UrlName = node.InnerText;
                        break;

                    case "distance":
                        if (double.TryParse(node.InnerText, out double result))
                        {
                            info.Distance = result;
                        }
                        break;

                    case "time":
                        if (DateTime.TryParse(node.InnerText, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out DateTime time))
                        {
                            info.Time = time;
                        }
                        break;

                    case "keywords":
                        info.KeyWords = node.InnerText;
                        break;

                    case "trk":
                        info.Tracks.Add(new GpxTrack(node, info));
                        break;

                    default:
                        info.OtherProperties.Add(node.Name, node.InnerText);
                        break;
                }
            }
        }
    }
}