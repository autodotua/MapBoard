using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MapBoard.Extension.AMap
{
    public static class KeyManager
    {
        public static string Key => "51992dc41316f8af892f7a1709dada0d";
    }

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
                    break;

                case RouteType.Car:
                    break;

                default:
                    break;
            }
            throw new NotSupportedException();
        }

        public RouteInfo[] ParseRoute(RouteType type, string json)
        {
            JObject root = JObject.Parse(json);
            JArray jPaths = root["route"]["paths"] as JArray;
            List<RouteInfo> paths = new List<RouteInfo>();
            foreach (var jPath in jPaths)
            {
                try
                {
                    var path = new RouteInfo()
                    {
                        Distance = jPath["distance"] is JValue ? double.Parse(jPath["distance"].Value<string>()) : 0,
                        Duration = jPath["duration"] is JValue ? TimeSpan.FromSeconds(double.Parse(jPath["duration"].Value<string>())) : (TimeSpan?)null
                    };

                    paths.Add(path);
                    List<PathStepInfo> steps = new List<PathStepInfo>();

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
                        var step = new PathStepInfo()
                        {
                            Distance = jStep["distance"] is JValue ? double.Parse(jStep["distance"].Value<string>()) : 0,
                            Duration = jStep["duration"] is JValue ? TimeSpan.FromSeconds(double.Parse(jStep["duration"].Value<string>())) : (TimeSpan?)null,
                            Locations = locations.ToArray(),
                            Name = jStep["road"] is JValue ? jStep["road"].Value<string>() : null,
                        };
                        steps.Add(step);
                    }
                    path.Steps = steps.ToArray();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return paths.ToArray();
        }
    }

    public class AMapPoiEngine : IPoiEngine
    {
        public string Name => "高德";
        public bool IsGcj02 => true;

        public string GetUrl(string keyword, Location center, double radius)
        {
            return $"https://restapi.amap.com/v3/place/around?key={KeyManager.Key}&location={center.Longitude:0.000000},{center.Latitude:0.000000}&keywords={keyword}&sortrule=weight&radius={radius}&offset=100&page=1";
        }

        public string GetUrl(string keyword, Location leftTop, Location rightBottom)
        {
            string rect = $"{leftTop.Longitude:0.000000},{leftTop.Latitude:0.000000}|{rightBottom.Longitude:0.000000},{rightBottom.Latitude:0.000000}";
            return $"https://restapi.amap.com/v3/place/polygon?key={KeyManager.Key}&polygon={rect}&keywords={keyword}&sortrule=weight&offset=100&page=1";
        }

        public PoiInfo[] ParsePois(string json)
        {
            JObject root = JObject.Parse(json);
            JArray jPois = root["pois"] as JArray;
            List<PoiInfo> pois = new List<PoiInfo>();
            foreach (var jPoi in jPois)
            {
                try
                {
                    var location = jPoi["location"].Value<string>().Split(',');
                    pois.Add(new PoiInfo()
                    {
                        Name = jPoi["name"] is JValue ? jPoi["name"].Value<string>() : null,
                        Address = jPoi["address"] is JValue ? jPoi["address"].Value<string>() : null,
                        Location = new Location(double.Parse(location[0]), double.Parse(location[1])),
                        Province = jPoi["pname"].Value<string>(),
                        City = jPoi["cityname"].Value<string>(),
                        Distance = jPoi["distance"] is JValue ? double.Parse(jPoi["distance"].Value<string>()) : (double?)null,
                    });
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return pois.ToArray();
        }
    }
}