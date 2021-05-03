using FzLib.Extension;
using FzLib.Geography.IO.Tile;
using GIS.Geometry;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GeoPoint = NetTopologySuite.Geometries.Point;

namespace MapBoard.TileDownloaderSplicer
{
    public class DownloadInfo : INotifyPropertyChanged
    {
        public Range<double> MapRange { get; private set; } = new Range<double>();

        //先Max后Min这样序列化的时候才不会出错
        private int tileMaxLevel = 2;

        public int TileMaxLevel
        {
            get => tileMaxLevel;
            set
            {
                if (value >= TileMinLevel)
                {
                    tileMaxLevel = value;
                }
                this.Notify(nameof(TileMaxLevel));
            }
        }

        private int tileMinLevel = 2;

        public int TileMinLevel
        {
            get => tileMinLevel;
            set
            {
                if (value <= TileMaxLevel)
                {
                    tileMinLevel = value;
                }
                this.Notify(nameof(TileMinLevel));
            }
        }

        public Dictionary<int, Range<int>> tiles = new Dictionary<int, Range<int>>();

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public IReadOnlyDictionary<int, Range<int>> Tiles { get; private set; }

        public void SetRange(Range<double> range)
        {
            MapRange = range;
            int count = 0;
            tiles.Clear();
            for (int level = TileMinLevel; level <= TileMaxLevel; level++)
            {
                var (tile1X, tile1Y) = TileLocation.GeoPointToTile(new GeoPoint(range.XMin_Left, range.YMax_Top), level);
                var (tile2X, tile2Y) = TileLocation.GeoPointToTile(new GeoPoint(range.XMax_Right, range.YMin_Bottom), level);

                tiles.Add(level, new Range<int>(tile1X, tile2X, tile1Y, tile2Y));

                count += (tile2X - tile1X + 1) * (tile2Y - tile1Y + 1);
            }
            Tiles = new ReadOnlyDictionary<int, Range<int>>(tiles);
            TileCount = count;
        }

        public IEnumerator<TileInfo> GetEnumerator()
        {
            return new TileEnumerator(tiles);
        }

        public IEnumerator<TileInfo> GetEnumerator(TileInfo start)
        {
            return new TileEnumerator(tiles, start);
        }

        public int TileCount { get; private set; }

        public class TileEnumerator : IEnumerator<TileInfo>
        {
            public TileEnumerator(Dictionary<int, Range<int>> ranges, TileInfo start = null)
            {
                Ranges = new Dictionary<int, Range<int>>(ranges);
                if (start != null)
                {
                    current = start;
                    currentRange = ranges[current.Level];
                }
            }

            private TileInfo current;
            public TileInfo Current => current;
            public Dictionary<int, Range<int>> Ranges { get; }
            object IEnumerator.Current => current;

            private Range<int> currentRange = null;

            public bool MoveNext()
            {
                if (Ranges == null || Ranges.Count == 0)
                {
                    return false;
                }
                int level;
                int x;
                int y;
                if (current == null)
                {
                    level = Ranges.Keys.Min();
                    currentRange = Ranges[level];
                    x = currentRange.XMin_Left;
                    y = currentRange.YMin_Bottom;
                    current = new TileInfo(level, x, y);
                    return true;
                }

                level = current.Level;
                x = current.X;
                y = current.Y + 1;

                if (y > currentRange.YMax_Top)
                {
                    y = currentRange.YMin_Bottom;
                    x++;
                    if (x > currentRange.XMax_Right)
                    {
                        level++;
                        if (level > Ranges.Keys.Max())
                        {
                            return false;
                        }
                        currentRange = Ranges[level];
                        x = currentRange.XMin_Left;
                        y = currentRange.YMin_Bottom;
                    }
                }
                current = new TileInfo(level, x, y);
                return true;
            }

            public void Reset()
            {
                current = null;
            }

            public void Dispose()
            {
            }
        }
    }

    public class TileInfo
    {
        public TileInfo()
        {
        }

        public TileInfo(int level, int x, int y)
        {
            Level = level;
            X = x;
            Y = y;
        }

        public int Level { get; }
        public int X { get; }
        public int Y { get; }

        public Range<double> Extent
        {
            get
            {
                (double yMax, double xMin) = TileLocation.PixelToGeoPoint(0, 0, X, Y, Level);
                // var prj = GeometryEngine.Project(new MapPoint(lat, lng, SpatialReferences.Wgs84), SpatialReferences.WebMercator) as MapPoint;
                (double yMin, double xMax) = TileLocation.PixelToGeoPoint(Config.Instance.TileSize.width, Config.Instance.TileSize.height, X, Y, Level);

                return new Range<double>(xMin, xMax, yMin, yMax);
            }
        }
    }
}