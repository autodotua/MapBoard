namespace MapBoard.Models
{
    public class GeoShareConfig
    {
        public string Server { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string GroupName { get; set; }

        public bool IsEnabled { get; set; } = false;

        public bool ShareLocation { get; set; } = true;
    }
}