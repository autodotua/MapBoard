using Esri.ArcGISRuntime.Geometry;
using FzLib;
using MapBoard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace MapBoard.IO.Gpx
{
    /// <summary>
    /// GPX trkpt对象
    /// </summary>
    public class GpxPoint : ICloneable, INotifyPropertyChanged
    {
        public GpxPoint(double x, double y, double? z, DateTime? time)
        {
            X = x;
            Y = y;
            Z = z;
            Time = time;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 其他属性
        /// </summary>
        public Dictionary<string, string> Extensions { get; internal set; } = new Dictionary<string, string>();

        /// <summary>
        /// 速度，需要计算后得出
        /// </summary>
        public double Speed { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public DateTime? Time { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// 高程
        /// </summary>
        public double? Z { get; set; }

        public object Clone()
        {
            return new GpxPoint(X, Y, Z, Time)
            {
                Extensions = Extensions.ToDictionary(p => p.Key, p => p.Value)
            };
        }

        /// <summary>
        /// 转为ArcGIS的点
        /// </summary>
        /// <returns></returns>
        public MapPoint ToMapPoint()
        {
            if(!Z.HasValue)
            {
                throw new InvalidOperationException("指定的" + nameof(GpxPoint) + "没有高度信息");
            }
            return new MapPoint(X, Y, Z.Value, SpatialReferences.Wgs84);
        }

        /// <summary>
        /// 转为不含高程的ArcGIS的点
        /// </summary>
        /// <returns></returns>
        public MapPoint ToXYMapPoint()
        {
            return new MapPoint(X, Y, SpatialReferences.Wgs84);
        }

        /// <summary>
        /// 从XML进行解析
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        internal static GpxPoint FromXml(XmlNode xml)
        {
            double x = 0;
            double y = 0;
            double? z = null;
            DateTime? time = null;
            Dictionary<string, string> otherProperties = new Dictionary<string, string>();
            foreach (var nodes in new IEnumerable<XmlNode>[] {
                xml.Attributes.Cast<XmlAttribute>(),
                xml.ChildNodes.Cast<XmlElement>() })
            {
                foreach (XmlNode node in nodes)
                {
                    switch (node.Name)
                    {
                        case "lat":
                            y = double.Parse(node.InnerText);
                            break;

                        case "lon":
                            x = double.Parse(node.InnerText);
                            break;

                        case "ele":
                            if (double.TryParse(node.InnerText, out double result))
                            {
                                z = result;
                            }
                            break;

                        case "time":
                            if (DateTime.TryParse(node.InnerText, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal, out DateTime t))
                            {
                                time = t;
                            }
                            break;

                        default:
                            otherProperties.Add(node.Name, node.InnerText);
                            break;
                    }
                }
            }
            var point = new GpxPoint(x, y, z, time)
            {
                Extensions = otherProperties
            };
            return point;
        }

        /// <summary>
        /// 将点转换为XML字符串
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="trkpt"></param>
        internal void WriteGpxXml(XmlDocument doc, XmlElement trkpt)
        {
            trkpt.SetAttribute("lat", Y.ToString());
            trkpt.SetAttribute("lon", X.ToString());
            if (Z.HasValue)
            {
                AppendChildNode("ele", Z.ToString());
            }
            if (Time.HasValue)
            {
                AppendChildNode("time", Time.Value.ToString(Gpx.GpxTimeFormat));
            }
            foreach (var item in Extensions)
            {
                AppendChildNode(item.Key, item.Value);
            }


            void AppendChildNode(string name, string value)
            {
                XmlElement child = doc.CreateElement(name);
                child.InnerText = value;
                trkpt.AppendChild(child);
            }
        }
    }
}