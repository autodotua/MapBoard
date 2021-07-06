using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace MapBoard.IO.Gpx
{
    public class Gpx : ICloneable
    {
        internal const string GpxTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        public static Gpx FromFile(string path)
        {
            return FromString(File.ReadAllText(path));
        }

        public static Gpx FromString(string gpxString)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(gpxString);
            XmlElement xmlGpx = xmlDoc["gpx"];
            if (xmlGpx == null)
            {
                throw new XmlException("没有找到gpx元素");
            }
            Gpx info = new Gpx(xmlGpx);

            return info;
        }

        private Gpx(XmlElement xml)
        {
            LoadGpxInfoProperties(this, xml);
        }

        public Gpx()
        {
        }

        public string Creator { get; set; }
        public string Version { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }

        public string UrlName { get; set; }
        public DateTime Time { get; set; }
        public string KeyWords { get; set; }
        public double Distance { get; set; }

        public Dictionary<string, string> OtherProperties { get; private set; } = new Dictionary<string, string>();

        public List<GpxTrack> Tracks { get; private set; } = new List<GpxTrack>();

        public static void LoadGpxInfoProperties(Gpx info, XmlNode xml)
        {
            LoadGpxInfoProperties(info, xml.Attributes.Cast<XmlAttribute>());
            LoadGpxInfoProperties(info, xml.ChildNodes.Cast<XmlElement>());
        }

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
            AppendChildNode("distance", Distance.ToString());
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
                    using (var xmlTextWriter = XmlWriter.Create(stringWriter, xmlSettingsWithIndentation))
                    {
                        doc.WriteTo(xmlTextWriter);
                        xmlTextWriter.Flush();
                        xmlString = stringWriter.GetStringBuilder().ToString();
                    }
                }

                return xmlString;
            }
        }

        public void Save(string path)
        {
            File.WriteAllText(path, ToGpxXml());
        }

        public object Clone()
        {
            var info = MemberwiseClone() as Gpx;
            info.Tracks = Tracks.Select(p => p.Clone() as GpxTrack).ToList();
            return info;
        }
    }
}