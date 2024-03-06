using MapBoard.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using XmpCore.Impl;

namespace MapBoard.IO.Gpx
{
    /// <summary>
    /// GPX数据类型
    /// </summary>
    public class Gpx : ICloneable
    {
        public Gpx()
        {
        }

        public string Author { get; set; }

        public string Creator { get; set; }

        public string Description { get; set; }

        public double Distance { get; set; }

        public TimeSpan Duration { get; set; }
        public Dictionary<string, string> Extensions { get; private set; } = new Dictionary<string, string>();

        public string FilePath { get; internal set; }

        public string KeyWords { get; set; }

        public string Name { get; set; }

        public DateTime Time { get; set; }
        public List<GpxTrack> Tracks { get; private set; } = new List<GpxTrack>();

        public string Url { get; set; }

        public string Version { get; set; }

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


    }
}