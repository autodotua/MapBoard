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
    /// GPX trk对象
    /// </summary>
    public class GpxTrack : IGpxElement
    {
        private static readonly string[] hiddenElements = ["cmt", "src", "link", "number", "type"];
        internal GpxTrack(Gpx parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// 轨迹描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 其他属性
        /// </summary>
        public Dictionary<string, string> Extensions { get; set; } = new Dictionary<string, string>();

        public string[] HiddenElements => hiddenElements;
        /// <summary>
        /// 轨迹名
        /// </summary>
        public string Name { get; set; }
        public IList<GpxSegment> Segments { get; private set; } = new List<GpxSegment>();

        /// <summary>
        /// 对应的GPX对象
        /// </summary>
        internal Gpx Parent { get; set; }
        public object Clone()
        {
            GpxTrack newObj = MemberwiseClone() as GpxTrack;
            newObj.Segments = Segments.Select(p => p.Clone() as GpxSegment).ToList();
            return newObj;
        }

        public GpxSegment CreateSegment()
        {
            var segment = new GpxSegment(this);
            Segments.Add(segment);
            return segment;
        }
    }
}