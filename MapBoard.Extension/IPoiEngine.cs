using System;
using System.Collections.Generic;

namespace MapBoard.Extension
{
    public interface IPoiEngine
    {
        /// <summary>
        /// 获取周边搜索的网址
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="centerLongitude">中心点经度</param>
        /// <param name="centerLatitude">中心点纬度</param>
        /// <param name="radius">半径</param>
        /// <returns></returns>
        string GetUrl(
           string keyword,
           double centerLongitude,
           double centerLatitude,
           double radius);

        /// <summary>
        /// 获取范围搜索的网址
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="leftTopLongitude">左上角经度</param>
        /// <param name="leftTopLatitude">左上角纬度</param>
        /// <param name="rightBottomLongitude">右下角经度</param>
        /// <param name="rightBottomLatitude">右下角纬度</param>
        /// <returns></returns>
        string GetUrl(
           string keyword,
           double leftTopLongitude,
           double leftTopLatitude,
           double rightBottomLongitude,
           double rightBottomLatitude);

        /// <summary>
        /// 根据范围的字符串，解析为<see cref="PoiInfo"/>
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        PoiInfo[] ParsePois(string json);

        /// <summary>
        /// POI搜索引擎名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 是否接受和返回GCJ02坐标而不是WGS84坐标
        /// </summary>
        bool IsGcj02 { get; }
    }
}