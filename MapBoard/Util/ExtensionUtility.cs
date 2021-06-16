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
    public static class ExtensionUtility
    {
        private static List<IPoiEngine> poiEngines = new List<IPoiEngine>();
        private static List<IRouteEngine> routeEngines = new List<IRouteEngine>();

        /// <summary>
        /// 加载到的所有POI引擎
        /// </summary>
        public static IReadOnlyList<IPoiEngine> PoiEngines => poiEngines.AsReadOnly();

        public static IReadOnlyList<IRouteEngine> RouteEngines => routeEngines.AsReadOnly();

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
                    List<Type> tempTypes = Assembly.LoadFile(dll)
                          .GetTypes()
                          .Where(p => p.GetInterface(typeof(IPoiEngine).FullName) != null)
                          .ToList();
                    if (tempTypes.Count > 0)
                    {
                        poiEngines.AddRange(tempTypes.Select(t => (IPoiEngine)Activator.CreateInstance(t)));
                    }

                    tempTypes = Assembly.LoadFile(dll)
                         .GetTypes()
                         .Where(p => p.GetInterface(typeof(IRouteEngine).FullName) != null)
                         .ToList();
                    if (tempTypes.Count > 0)
                    {
                        routeEngines.AddRange(tempTypes.Select(t => (IRouteEngine)Activator.CreateInstance(t)));
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
            CoordinateSystem target = poi.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;
            center = CoordinateTransformation.Transformate(center, Config.Instance.BasemapCoordinateSystem, target);
            string url = poi.GetUrl(keyword, center.ToLocation(), radius);
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
            CoordinateSystem target = poi.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;
            rect = CoordinateTransformation.Transformate(rect, Config.Instance.BasemapCoordinateSystem, target) as Envelope;

            string url = poi.GetUrl(keyword, new Location(rect.XMin, rect.YMax), new Location(rect.XMax, rect.YMin));
            return await GetPoisAsync(poi, url);
        }

        /// <summary>
        /// 范围搜索
        /// </summary>
        /// <param name="poi"></param>
        /// <param name="keyword"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static async Task<RouteInfo[]> SearchRouteAsync(this IRouteEngine route, RouteType type, MapPoint origin, MapPoint destination)
        {
            CoordinateSystem target = route.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;
            if (target != Config.Instance.BasemapCoordinateSystem)
            {
                origin = CoordinateTransformation.Transformate(origin, Config.Instance.BasemapCoordinateSystem, target);
                destination = CoordinateTransformation.Transformate(destination, Config.Instance.BasemapCoordinateSystem, target);
            }

            string url = route.GetUrl(type, origin.ToLocation(), destination.ToLocation());
            return await GetRoutesAsync(route, type, url);
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
            string json = await HttpGetAsync(url);
            var pois = pe.ParsePois(json);
            CoordinateSystem source = pe.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;

            if (source != Config.Instance.BasemapCoordinateSystem)
            {
                foreach (var poi in pois)
                {
                    var wgs = CoordinateTransformation.Transformate(new MapPoint(poi.Location.Longitude, poi.Location.Latitude, SpatialReferences.Wgs84), source, Config.Instance.BasemapCoordinateSystem);
                    poi.Location = new Location(wgs.X, wgs.Y);
                }
            }
            return pois;
        }

        /// <summary>
        /// 获取路径规划
        /// </summary>
        /// <param name="pe"></param>
        /// <param name="type"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<RouteInfo[]> GetRoutesAsync(IRouteEngine pe, RouteType type, string url)
        {
            string json = await HttpGetAsync(url);
            RouteInfo[] paths = null;
            await Task.Run(() =>
            {
                paths = pe.ParseRoute(type, json);
                CoordinateSystem source = pe.IsGcj02 ? CoordinateSystem.GCJ02 : CoordinateSystem.WGS84;

                if (source != Config.Instance.BasemapCoordinateSystem)
                {
                    foreach (var path in paths)
                    {
                        foreach (var step in path.Steps)
                        {
                            foreach (var loc in step.Locations)
                            {
                                var wgs = CoordinateTransformation.Transformate(new MapPoint(loc.Longitude, loc.Latitude, SpatialReferences.Wgs84), source, Config.Instance.BasemapCoordinateSystem);
                                loc.Longitude = wgs.X;
                                loc.Latitude = wgs.Y;
                            }
                        }
                    }
                }
            });
            return paths;
        }

        /// <summary>
        /// 从指定网址GET数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static async Task<string> HttpGetAsync(string url)
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

        public static MapPoint ToMapPoint(this Location location)
        {
            return new MapPoint(location.Longitude, location.Latitude, SpatialReferences.Wgs84);
        }

        public static Location ToLocation(this MapPoint point)
        {
            return new Location(point.X, point.Y);
        }
    }
}