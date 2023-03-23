using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MapBoard.Extension.AMap
{
    public class AMapPAoiEngine : IReGeoCodeEngine
    {
        public string Name => "高德";
        public bool IsGcj02 => true;
        private string token;
        public string Token
        {
            get => token ?? throw new ApiException("请先设置Token");
            set => token = value;
        }
        public string GetUrl(Location location, double radius)
        {
            return $"https://restapi.amap.com/v3/geocode/regeo?key={Token}&location={location.Longitude:0.000000},{location.Latitude:0.000000}&poitype=&radius={radius}&extensions=all&batch=false&roadlevel=0";
        }

        public LocationInfo ParseLocationInfo(string json)
        {
            JObject root = JObject.Parse(json);
            ApiChecker.CheckResponse(root);
            try
            {
                LocationInfo result = new LocationInfo();
                JObject code = root["regeocode"] as JObject;
                result.Address = code.TryGetValue<string>("formatted_address");

                var jPois = code["pois"] as JArray;
                result.Pois = AMapPoiEngine.ParsePois(jPois);

                List<RoadInfo> roads = new List<RoadInfo>();
                var jRoads = code["roads"] as JArray;
                foreach (var jRoad in jRoads)
                {
                    roads.Add(new RoadInfo()
                    {
                        Name = jRoad.TryGetValue<string>("name"),
                        Location = jRoad.TryGetValue<string>("location").ToLocation(),
                    });
                }
                result.Roads = roads.ToArray();

                var jAddress = code["addressComponent"] as JObject;
                result.Administrative = new AdministrativeInfo()
                {
                    Country = jAddress.TryGetValue<string>("country"),
                    City = jAddress.TryGetValue<string>("city"),
                    CityCode = jAddress.TryGetValue<string>("citycode"),
                    Province = jAddress.TryGetValue<string>("province"),
                    Code = jAddress.TryGetValue<string>("adcode"),
                    District = jAddress.TryGetValue<string>("district"),
                    TownShip = jAddress.TryGetValue<string>("township"),
                };
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("解析失败", ex);
            }
        }
    }
}