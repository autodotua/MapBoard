using Esri.ArcGISRuntime.Geometry;
using MapBoard.Common;
using MapBoard.Extension;
using MapBoard.Main.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapBoard.Main.Util
{
    public static class PoiUtility
    {
        private static List<IPoiEngine> poiEngines = new List<IPoiEngine>();

        /// <summary>
        /// 加载到的所有POI引擎
        /// </summary>
        public static IReadOnlyList<IPoiEngine> PoiEngines => poiEngines.AsReadOnly();

        private static bool isLoaded = false;

        /// <summary>
        /// 初始化POI引擎扩展
        /// </summary>
        public static void LoadExtensions()
        {
            //Regex r = new Regex(@"Extension\.[a-zA-Z0-9]+\.dll");
            var dlls = Directory.EnumerateFiles(FzLib.Program.App.ProgramDirectoryPath, "Extension.*.dll");
            foreach (var dll in dlls)
            {
                try
                {
                    var tempTypes = Assembly.LoadFile(dll)
                          .GetTypes()
                          .Where(p => p.GetInterface(typeof(IPoiEngine).FullName) != null)
                          .ToList();
                    if (tempTypes.Count > 0)
                    {
                        poiEngines.AddRange(tempTypes.Select(t => (IPoiEngine)Activator.CreateInstance(t)));
                    }
                }
                catch
                {
                }
            }
            isLoaded = true;
        }

        /// <summary>
        /// 周边搜索
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="keyword"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static async Task<PoiInfo[]> SearchAsync(this IPoiEngine poi, string keyword, MapPoint center, double radius)
        {
            if (!isLoaded)
            {
                throw new Exception("还未加载POI引擎");
            }
            string url = poi.GetUrl(keyword, center.X, center.Y, radius);
            return await GetPoisAsync(poi, url);
        }

        /// <summary>
        /// 范围搜索
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="keyword"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static async Task<PoiInfo[]> SearchAsync(this IPoiEngine poi, string keyword, Envelope rect)
        {
            string url = poi.GetUrl(keyword, rect.XMin, rect.YMax, rect.XMax, rect.YMin);
            return await GetPoisAsync(poi, url);
        }

        /// <summary>
        /// 根据搜索引擎和网址，获得POI信息
        /// </summary>
        /// <param name="pe"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<PoiInfo[]> GetPoisAsync(IPoiEngine pe, string url)
        {
            if (!isLoaded)
            {
                throw new Exception("还未加载POI引擎");
            }
            string json = await GetAsync(url);
            var pois = pe.ParsePois(json);
            CoordinateSystem source = pe.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;

            if (source != Config.Instance.BasemapCoordinateSystem)
            {
                foreach (var poi in pois)
                {
                    var wgs = CoordinateTransformation.Transformate(new MapPoint(poi.Longitude, poi.Latitude, SpatialReferences.Wgs84), source, Config.Instance.BasemapCoordinateSystem);
                    poi.Longitude = wgs.X;
                    poi.Latitude = wgs.Y;
                }
            }
            return pois;
        }

        /// <summary>
        /// 从指定网址GET数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<string> GetAsync(string url)
        {
            // 设置参数
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Timeout = Config.Instance.RequestTimeOut;
            request.ReadWriteTimeout = Config.Instance.ReadTimeOut;
            request.UserAgent = Config.Instance.UserAgent;

            using HttpWebResponse response = (await request.GetResponseAsync()) as HttpWebResponse;
            using var responseStream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(responseStream);
            string text = reader.ReadToEnd();
            return text;
        }
    }
}