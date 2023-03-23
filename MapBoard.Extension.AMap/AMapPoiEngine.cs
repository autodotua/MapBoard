using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MapBoard.Extension.AMap
{
    public class AMapPoiEngine : IPoiEngine
    {
        public AMapPoiEngine()
        {
        }

        public string Name => "高德";
        public bool IsGcj02 => true;
        private string token;
        public string Token
        {
            get => token ?? throw new ApiException("请先设置Token");
            set => token = value;
        }


        public string GetUrl(string keyword, Location center, double radius)
        {
            return $"https://restapi.amap.com/v3/place/around?key={Token}&location={center.Longitude:0.000000},{center.Latitude:0.000000}&keywords={keyword}&sortrule=weight&radius={radius}&offset=100&page=1";
        }

        public string GetUrl(string keyword, Location leftTop, Location rightBottom)
        {
            string rect = $"{leftTop.Longitude:0.000000},{leftTop.Latitude:0.000000}|{rightBottom.Longitude:0.000000},{rightBottom.Latitude:0.000000}";
            return $"https://restapi.amap.com/v3/place/polygon?key={Token}&polygon={rect}&keywords={keyword}&sortrule=weight&offset=100&page=1";
        }

        public PoiInfo[] ParsePois(string json)
        {
            JObject root = JObject.Parse(json);
            ApiChecker.CheckResponse(root);
            try
            {
                JArray jPois = root["pois"] as JArray;
                return ParsePois(jPois);
            }
            catch (Exception ex)
            {
                throw new Exception("解析失败", ex);
            }
        }

        internal static PoiInfo[] ParsePois(JArray jPois)
        {
            List<PoiInfo> pois = new List<PoiInfo>();
            foreach (var jPoi in jPois)
            {
                pois.Add(new PoiInfo()
                {
                    Name = jPoi.TryGetValue<string>("name"),
                    Address = jPoi["address"] is JValue ? jPoi["address"].Value<string>() : null,
                    Type = jPoi.TryGetValue<string>("type"),
                    Location = jPoi.TryGetValue<string>("location").ToLocation(),
                    Province = jPoi.TryGetValue<string>("pname"),
                    City = jPoi.TryGetValue<string>("cityname"),
                    Distance = jPoi["distance"] is JValue ? double.Parse(jPoi["distance"].Value<string>()) : (double?)null,
                });
            }
            return pois.ToArray();
        }
    }
}