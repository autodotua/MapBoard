using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MapBoard.Extension.AMap
{
    public class AMapRouteEngine : IRouteEngine
    {
        public string Name => "高德";
        public bool IsGcj02 => true;

        public string GetUrl(RouteType type, Location origin, Location destination)
        {
            string strOrigin = $"{origin.Longitude:0.000000},{origin.Latitude:0.000000}";
            string strDestination = $"{destination.Longitude:0.000000},{destination.Latitude:0.000000}";
            switch (type)
            {
                case RouteType.Walk:
                    return $"https://restapi.amap.com/v3/direction/walking?origin={strOrigin}&destination={strDestination}&key={KeyManager.Key}";

                case RouteType.Bike:
                    return $"https://restapi.amap.com/v4/direction/bicycling?origin={strOrigin}&destination={strDestination}&key={KeyManager.Key}";

                case RouteType.Car:
                    return $"https://restapi.amap.com/v3/direction/driving?strategy=11&origin={strOrigin}&destination={strDestination}&extensions=base&key={KeyManager.Key}";

                default:
                    break;
            }
            throw new NotSupportedException();
        }

        public RouteInfo[] ParseRoute(RouteType type, string json)
        {
            JObject root = JObject.Parse(json);
            ApiChecker.CheckResponse(root);
            try
            {
                JArray jPaths = (root["route"] ?? root["data"])["paths"] as JArray;
                List<RouteInfo> paths = new List<RouteInfo>();
                foreach (var jPath in jPaths)
                {
                    var path = new RouteInfo()
                    {
                        Distance = jPath["distance"] is JValue ? double.Parse(jPath["distance"].Value<string>()) : 0,
                        Strategy = jPath["strategy"] is JValue ? jPath["strategy"].Value<string>() : null,
                        Duration = jPath["duration"] is JValue ? TimeSpan.FromSeconds(double.Parse(jPath["duration"].Value<string>())) : (TimeSpan?)null
                    };

                    paths.Add(path);
                    List<RouteStepInfo> steps = new List<RouteStepInfo>();

                    foreach (JObject jStep in jPath["steps"])
                    {
                        string strLocations = jStep["polyline"].Value<string>();
                        List<Location> locations = new List<Location>();
                        foreach (var strLocation in strLocations.Split(';'))
                        {
                            var strLonLat = strLocation.Split(',');
                            Location l = new Location()
                            {
                                Longitude = double.Parse(strLonLat[0]),
                                Latitude = double.Parse(strLonLat[1])
                            };
                            locations.Add(l);
                        }
                        var step = new RouteStepInfo()
                        {
                            Distance = jStep["distance"] is JValue ? double.Parse(jStep["distance"].Value<string>()) : 0,
                            Duration = jStep["duration"] is JValue ? TimeSpan.FromSeconds(double.Parse(jStep["duration"].Value<string>())) : (TimeSpan?)null,
                            Locations = locations.ToArray(),
                            Road = jStep["road"] is JValue ? jStep["road"].Value<string>() : null,
                        };
                        steps.Add(step);
                    }
                    path.Steps = steps.ToArray();
                }
                return paths.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("解析失败", ex);
            }
        }
    }
}