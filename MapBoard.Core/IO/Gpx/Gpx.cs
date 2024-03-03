using MapBoard.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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


        public string Author { get; set; }

        public string Creator { get; set; }

        public string Description { get; set; }

        public double Distance { get; set; }

        public string FilePath { get; private set; }

        public string KeyWords { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> OtherProperties { get; private set; } = new Dictionary<string, string>();

        public DateTime Time { get; set; }

        public List<GpxTrack> Tracks { get; private set; } = new List<GpxTrack>();

        public string Url { get; set; }

        public string Version { get; set; }

        /// <summary>
        /// 从文件加载GPX
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task FromFileAsync(string path)
        {
            await Task.Run(() =>
            {
                var xmlString = File.ReadAllText(path);
                LoadFromString(xmlString, path);
            });
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



        /// <summary>
        /// 从文件和读取后的内容加载GPX
        /// </summary>
        /// <param name="xmlString"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="XmlException"></exception>
        public void LoadFromString(string xmlString, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            XmlElement gpxNode = xmlDoc["gpx"];
            if (gpxNode == null)
            {
                throw new XmlException("没有找到gpx元素");
            }
            FilePath = path;
            foreach (XmlAttribute attribute in gpxNode.Attributes)
            {
                SetValue(attribute.Name, attribute.Value);
            } 
            //老版本的GPX类错误地将metadata中的元数据放在了gpx节点下
            foreach (XmlElement element in gpxNode.ChildNodes)
            {
                SetValue(element.Name, element.Value);
            }
            //读取元数据
            if (gpxNode["metadata"] != null)
            {
                foreach (XmlElement element in gpxNode["metadata"].ChildNodes)
                {
                    SetValue(element.Name, element.Value);
                }
            }
            if (gpxNode["trk"] != null)
            {
                throw new NotImplementedException();
                //GpxTrack.LoadGpxTrackInfoProperties()
            }

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

            XmlElement root = doc.CreateElement("gpx");
            doc.AppendChild(root);
            XmlElement metadata = doc.CreateElement("metadata");
            root.AppendChild(metadata);

            root.SetAttribute("version", Version);
            root.SetAttribute("creator", Creator);
            AppendToMetadata("name", Name);
            AppendToMetadata("author", Author);
            AppendToMetadata("url", Url);
            AppendToMetadata("time", Time.ToString(GpxTimeFormat));
            AppendToMetadata("keywords", KeyWords);
            AppendToMetadata("distance", Math.Round(Tracks.Sum(p => p.Distance), 2).ToString());
            foreach (var item in OtherProperties)
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
            foreach (var trk in Tracks)
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


        private static readonly Dictionary<string, Action<Gpx, string>> xml2Property = new Dictionary<string, Action<Gpx, string>>()
        {
            ["creator"] = (g, v) => g.Creator = v,
            ["version"] = (g, v) => g.Version = v,
            ["name"] = (g, v) => g.Name = v,
            ["author"] = (g, v) => g.Author = v,
            ["url"] = (g, v) => g.Url = v,
            ["distance"] = (g, v) => { if (double.TryParse(v, out double result)) g.Distance = result; },
            ["time"] = (g, v) => { if (DateTime.TryParse(v, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out DateTime time)) g.Time = time; },
            ["keywords"] = (g, v) => g.KeyWords = v,
            ["trk"] = (g, v) => { }// Tracks.Add()
        };
        private void SetValue(string key, string value)
        {
            if (xml2Property.TryGetValue(key, out Action<Gpx, string> func))
            {
                func(this, value);
            }
        }
    }
}