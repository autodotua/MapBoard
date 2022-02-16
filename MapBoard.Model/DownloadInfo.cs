using FzLib;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MapBoard.Model
{
    public class DownloadInfo : INotifyPropertyChanged
    {
        public IDictionary<int, GeoRect<int>> tiles = new Dictionary<int, GeoRect<int>>();

        //先Max后Min这样序列化的时候才不会出错
        private int tileMaxLevel = 2;

        private int tileMinLevel = 2;

        public event PropertyChangedEventHandler PropertyChanged;

        public GeoRect<double> MapRange { get; set; } = new GeoRect<double>();
        public int TileCount { get; private set; }

        public int TileMaxLevel
        {
            get => tileMaxLevel;
            set
            {
                if (value >= TileMinLevel)
                {
                    tileMaxLevel = value;
                }
            }
        }

        public int TileMinLevel
        {
            get => tileMinLevel;
            set
            {
                if (value <= TileMaxLevel)
                {
                    tileMinLevel = value;
                }
            }
        }

        [JsonIgnore]
        public IReadOnlyDictionary<int, GeoRect<int>> Tiles { get; private set; }

        public IEnumerator<TileInfo> GetEnumerator()
        {
            return new TileEnumerator(tiles);
        }

        public IEnumerator<TileInfo> GetEnumerator(TileInfo start)
        {
            return new TileEnumerator(tiles, start);
        }

        public void SetRange(GeoRect<double> range, IDictionary<int, GeoRect<int>> tiles, int count)
        {
            MapRange = range;
            this.tiles = tiles;
            Tiles = new ReadOnlyDictionary<int, GeoRect<int>>(tiles);

            TileCount = count;
        }

        public class TileEnumerator : IEnumerator<TileInfo>
        {
            private TileInfo current;

            private GeoRect<int> currentRange = null;

            public TileEnumerator(IDictionary<int, GeoRect<int>> ranges, TileInfo start = null)
            {
                Ranges = new Dictionary<int, GeoRect<int>>(ranges);
                if (start != null)
                {
                    current = start;
                    currentRange = ranges[current.Level];
                }
            }

            public TileInfo Current => current;
            object IEnumerator.Current => current;
            public Dictionary<int, GeoRect<int>> Ranges { get; }

            public void Dispose()
            {
            }

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
        }
    }
}