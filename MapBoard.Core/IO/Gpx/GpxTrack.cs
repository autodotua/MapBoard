using Esri.ArcGISRuntime.Geometry;
using MapBoard;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MapBoard.IO.Gpx
{
    public class GpxTrack : ICloneable
    {
        internal Gpx GpxInfo { get; set; }

        internal GpxTrack(XmlNode xml, Gpx parent)
        {
            GpxInfo = parent;
            LoadGpxTrackInfoProperties(this, xml);
        }

        public Dictionary<string, string> OtherProperties { get; private set; } = new Dictionary<string, string>();
        public string Name { get; set; }
        public string Description { get; set; }
        public GpxPointCollection Points { get; private set; } = new GpxPointCollection();

        public double distance = -1;

        public double Distance
        {
            get
            {
                if (distance == -1)
                {
                    distance = 0;
                    MapPoint last = null;
                    foreach (var point in Points.TimeOrderedPoints)
                    {
                        if (last != null)
                        {
                            distance += GeometryUtility.GetDistance(last, point.ToMapPoint());
                        }
                        last = point.ToMapPoint();
                    }
                }
                return distance;
            }
        }

        public double AverageSpeed => Distance / TotalTime.TotalSeconds;
        private TimeSpan? totalTime = null;

        public TimeSpan TotalTime
        {
            get
            {
                if (totalTime == null)
                {
                    totalTime = Points.TimeOrderedPoints[Points.TimeOrderedPoints.Count - 1].Time - Points.TimeOrderedPoints[0].Time;
                }
                return totalTime.Value;
            }
        }

        private double maxSpeed = -1;

        public async Task<double> GetMaxSpeedAsync()
        {
            if (maxSpeed == -1)
            {
                maxSpeed = (await GpxSpeedAnalysis.GetSpeedsAsync(Points)).Max(p => p.Speed);
            }
            return maxSpeed;
        }

        public async Task<double> GetMaxSpeedAsync(int sampleCount = 8, int jump = 1)
        {
            return (await GpxSpeedAnalysis.GetMeanFilteredSpeedsAsync(Points, sampleCount, jump)).Max(p => p.Speed);
        }

        public TimeSpan GetMovingTime(double speedDevaluation = 0.3)
        {
            double totalDistance = 0;
            double totalSeconds = 0;
            GpxPoint last = null;
            foreach (var point in Points.TimeOrderedPoints)
            {
                if (last != null)
                {
                    double distance = GeometryUtility.GetDistance(last.ToMapPoint(), point.ToMapPoint());
                    double second = (point.Time - last.Time).TotalSeconds;
                    double speed = distance / second;
                    if (speed > speedDevaluation)
                    {
                        totalDistance += distance;
                        totalSeconds += second;
                    }
                }
                last = point;
            }
            return TimeSpan.FromSeconds(totalSeconds);
        }

        public double GetMovingAverageSpeed(double speedDevaluation = 0.3)
        {
            double totalDistance = 0;
            double totalSeconds = 0;
            GpxPoint last = null;
            foreach (var point in Points.TimeOrderedPoints)
            {
                if (last != null)
                {
                    double distance = GeometryUtility.GetDistance(last.ToMapPoint(), point.ToMapPoint());
                    double second = (point.Time - last.Time).TotalSeconds;
                    double speed = distance / second;
                    if (speed > speedDevaluation)
                    {
                        totalDistance += distance;
                        totalSeconds += second;
                    }
                }
                last = point;
            }
            return totalDistance / totalSeconds;
        }

        public static void LoadGpxTrackInfoProperties(GpxTrack info, XmlNode xml)
        {
            try
            {
                LoadGpxTrackInfoProperties(info, xml.Attributes.Cast<XmlAttribute>());
                LoadGpxTrackInfoProperties(info, xml.ChildNodes.Cast<XmlElement>());
            }
            catch (Exception ex)
            {
                throw new XmlException("解析轨迹失败", ex);
            }
        }

        private static void LoadGpxTrackInfoProperties(GpxTrack info, IEnumerable<XmlNode> nodes)
        {
            foreach (var node in nodes)
            {
                switch (node.Name)
                {
                    case "name":
                        info.Name = node.InnerText;
                        break;

                    case "desc":
                        info.Description = node.InnerText;
                        break;

                    case "trkseg":
                        foreach (XmlNode ptNode in node.ChildNodes)
                        {
                            GpxPoint point = GpxPoint.FromXml(ptNode);
                            //GpxTrackPoint.LoadGpxTrackPointInfoProperties(point, ptNode);
                            info.Points.Add(point);
                        }
                        break;

                    default:
                        info.OtherProperties.Add(node.Name, node.InnerText);
                        break;
                }
            }
        }

        internal void WriteGpxXml(XmlDocument doc, XmlNode trk)
        {
            AppendChildNode("name", Name);
            AppendChildNode("desc", Description);
            foreach (var item in OtherProperties)
            {
                AppendChildNode(item.Key, item.Value);
            }
            XmlElement pointsNode = doc.CreateElement("trkseg");
            trk.AppendChild(pointsNode);
            foreach (var point in Points)
            {
                XmlElement node = doc.CreateElement("trkpt");
                point.WriteGpxXml(doc, node);
                pointsNode.AppendChild(node);
            }
            void AppendChildNode(string name, string value)
            {
                XmlElement child = doc.CreateElement(name);
                child.InnerText = value;
                trk.AppendChild(child);
            }
        }

        public object Clone()
        {
            GpxTrack info = MemberwiseClone() as GpxTrack;
            info.Points = Points.Clone() as GpxPointCollection;
            return info;
        }
    }
}