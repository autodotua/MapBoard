namespace MapBoard.Extension
{
    public class LocationInfo
    {
        public string Address { get; set; }
        public AdministrativeInfo Administrative { get; set; }
        public PoiInfo[] Pois { get; set; }
        public RoadInfo[] Roads { get; set; }
    }

    public class AdministrativeInfo
    {
        public string Country { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Code { get; set; }
        public string CityCode { get; set; }
        public string TownShip { get; set; }
    }

    public class RoadInfo
    {
        public string Name { get; set; }
        public Location Location { get; set; }
    }
}