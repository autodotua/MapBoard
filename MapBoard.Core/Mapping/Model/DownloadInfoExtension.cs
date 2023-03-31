using Esri.ArcGISRuntime.Geometry;
using MapBoard.Model;
using MapBoard.Util;
using System.Collections.Generic;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 瓦片下载扩展类
    /// </summary>
    public static class DownloadInfoExtension
    {
        /// <summary>
        /// 根据地理范围设置需要下载的瓦片的信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="range"></param>
        public static void SetRange(this DownloadInfo info, GeoRect<double> range)
        {
            Dictionary<int, GeoRect<int>> tiles = new Dictionary<int, GeoRect<int>>();
            int count = 0;
            for (int level = info.TileMinLevel; level <= info.TileMaxLevel; level++)
            {
                var (tile1X, tile1Y) = TileLocationUtility.GeoPointToTile(new MapPoint(range.XMin_Left, range.YMax_Top), level);
                var (tile2X, tile2Y) = TileLocationUtility.GeoPointToTile(new MapPoint(range.XMax_Right, range.YMin_Bottom), level);

                tiles.Add(level, new GeoRect<int>(tile1X, tile2X, tile1Y, tile2Y));

                count += (tile2X - tile1X + 1) * (tile2Y - tile1Y + 1);
            }
            info.SetRange(range, tiles, count);
        }
    }
}