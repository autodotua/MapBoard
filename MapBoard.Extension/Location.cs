namespace MapBoard.Extension
{
    public class Location
    {
        public Location()
        {
        }

        public Location(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}