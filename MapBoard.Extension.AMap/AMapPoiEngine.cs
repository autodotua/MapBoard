using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MapBoard.Extension.AMap
{
    public class AMapPoiEngine : IPoiEngine
    {
        private string key = "51992dc41316f8af892f7a1709dada0d";

        public string Name => "高德";
        public bool IsGcj02 => true;

        public string GetUrl(string keyword, double centerLongitude, double centerLatitude, double radius)
        {
            return $"https://restapi.amap.com/v3/place/around?key={key}&location={centerLongitude:0.000000},{centerLatitude:0.000000}&keywords={keyword}&sortrule=weight&radius={radius}&offset=100&page=1";
        }

        public string GetUrl(string keyword, double leftTopLongitude, double leftTopLatitude, double rightBottomLongitude, double rightBottomLatitude)
        {
            string rect = $"{leftTopLongitude:0.000000},{leftTopLatitude:0.000000}|{rightBottomLongitude:0.000000},{rightBottomLatitude:0.000000}";
            return $"https://restapi.amap.com/v3/place/polygon?key={key}&polygon={rect}&keywords={keyword}&sortrule=weight&offset=100&page=1";
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
                        Longitude = double.Parse(location[0]),
                        Latitude = double.Parse(location[1]),
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