using System;
using System.Collections.Generic;

namespace MapBoard.Extension
{
    public interface IPoiEngine : IExtensionEngine
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
           Location center,
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
           Location leftTop,
           Location rightBottom);

        /// <summary>
        /// 根据范围的字符串，解析为<see cref="PoiInfo"/>
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        PoiInfo[] ParsePois(string json);
    }
}