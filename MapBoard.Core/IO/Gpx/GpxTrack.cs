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
    /// <summary>
    /// GPX单个轨迹
    /// </summary>
    public class GpxTrack : ICloneable
    {
        public double distance = -1;

        private double maxSpeed = -1;

        private TimeSpan? totalTime = null;

        internal GpxTrack(XmlNode xml, Gpx parent)
        {
            GpxInfo = parent;
            LoadGpxTrackInfoProperties(this, xml);
        }

        /// <summary>
        /// 平均速度
        /// </summary>
        public double AverageSpeed => Distance / TotalTime.TotalSeconds;

        /// <summary>
        /// 轨迹描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 总路程
        /// </summary>
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

        /// <summary>
        /// 轨迹名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 其他属性
        /// </summary>
        public Dictionary<string, string> OtherProperties { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// 轨迹点集
        /// </summary>
        public GpxPointCollection Points { get; private set; } = new GpxPointCollection();

        /// <summary>
        /// 总时长
        /// </summary>
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

        /// <summary>
        /// 对应的GPX对象
        /// </summary>
        internal Gpx GpxInfo { get; set; }
        /// <summary>
        /// 读取节点值，写入到对应的GPX属性
        /// </summary>
        /// <param name="info"></param>
        /// <param name="xml"></param>
        /// <exception cref="XmlException"></exception>
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

        public object Clone()
        {
            GpxTrack info = MemberwiseClone() as GpxTrack;
            info.Points = Points.Clone() as GpxPointCollection;
            return info;
        }

        /// <summary>
        /// 获取最大速度
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetMaxSpeedAsync()
        {
            if (maxSpeed == -1)
            {
                maxSpeed = (await GpxSpeedAnalysis.GetSpeedsAsync(Points)).Max(p => p.Speed);
            }
            return maxSpeed;
        }

        /// <summary>
        /// 根据提供的采样率参数，获取最大速度
        /// </summary>
        /// <param name="sampleCount"></param>
        /// <param name="jump"></param>
        /// <returns></returns>
        public async Task<double> GetMaxSpeedAsync(int sampleCount = 8, int jump = 1)
        {
            return (await GpxSpeedAnalysis.GetMeanFilteredSpeedsAsync(Points, sampleCount, jump)).Max(p => p.Speed);
        }

        /// <summary>
        /// 获取移动均速
        /// </summary>
        /// <param name="speedDevaluation"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取移动时间
        /// </summary>
        /// <param name="speedDevaluation"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 获取GPX的XML字符串
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="trk"></param>
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

        /// <summary>
        /// 读取节点值，写入到对应的GPX属性
        /// </summary>
        /// <param name="info"></param>
        /// <param name="nodes"></param>
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
    }
}