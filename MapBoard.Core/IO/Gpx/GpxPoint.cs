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
    public class GpxPoint : ICloneable, INotifyPropertyChanged
    {
        public GpxPoint(double x, double y, double z, DateTime time)
        {
            X = x;
            Y = y;
            Z = z;
            Time = time;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Dictionary<string, string> OtherProperties { get; internal set; } = new Dictionary<string, string>();

        public double Speed { get; set; }

        public DateTime Time { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public object Clone()
        {
            return new GpxPoint(X, Y, Z, Time)
            {
                OtherProperties = OtherProperties.ToDictionary(p => p.Key, p => p.Value)
            };
        }

        public MapPoint ToMapPoint()
        {
            return new MapPoint(X, Y, Z, SpatialReferences.Wgs84);
        }

        public MapPoint ToXYMapPoint()
        {
            return new MapPoint(X, Y, SpatialReferences.Wgs84);
        }

        internal static GpxPoint FromXml(XmlNode xml)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            DateTime time = default;
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
            var point = new GpxPoint(x, y, z, time);
            point.OtherProperties = otherProperties;
            return point;
        }

        internal void WriteGpxXml(XmlDocument doc, XmlElement trkpt)
        {
            trkpt.SetAttribute("lat", Y.ToString());
            trkpt.SetAttribute("lon", X.ToString());
            AppendChildNode("ele", Z.ToString());
            //foreach (var item in OtherProperties)
            //{
            //    AppendChildNode(item.Key, item.Value);
            //}

            AppendChildNode("time", Time.ToString(Gpx.GpxTimeFormat));

            void AppendChildNode(string name, string value)
            {
                XmlElement child = doc.CreateElement(name);
                child.InnerText = value;
                trkpt.AppendChild(child);
            }
        }
    }
}