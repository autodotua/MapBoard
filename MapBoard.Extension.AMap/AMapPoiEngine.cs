using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MapBoard.Extension.AMap
{
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
            ApiChecker.CheckResponse(root);
            try
            {
                JArray jPois = root["pois"] as JArray;
                List<PoiInfo> pois = new List<PoiInfo>();
                foreach (var jPoi in jPois)
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
                return pois.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("解析失败", ex);
            }
        }
    }
}