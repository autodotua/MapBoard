using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MapBoard.GeoShare.Core.Dto;
using MapBoard.GeoShare.Core.Entity;
using MapBoard.Util;
using MapBoard.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Services
{
    public class GeoShareService
    {
        HttpService httpService = new HttpService();

        PeriodicTimer timer;
        private readonly LocationDisplay locationDisplay;

        public GraphicsOverlay Overlay { get; }

        public GeoShareService(GraphicsOverlay overlay, LocationDisplay locationDisplay)
        {
            Overlay = overlay;
            this.locationDisplay = locationDisplay;
        }
        public async void Start()
        {
            timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            if (Config.Instance.GeoShare.IsEnabled)
            {
                try
                {
                    await LoginAsync();
                }
                catch (Exception ex)
                {
                    await MainPage.Current.DisplayAlert("位置共享登陆失败", ex.Message, "确定");
                    Config.Instance.GeoShare.IsEnabled = false;
                }
            }
            StartTimerAsync();
        }

        private async Task LoginAsync()
        {
            await httpService.PostAsync(Config.Instance.GeoShare.Server + HttpService.Url_Login, new UserEntity()
            {
                Username = Config.Instance.GeoShare.UserName,
                Password = Config.Instance.GeoShare.Password,
                GroupName = Config.Instance.GeoShare.GroupName,
            });
        }

        public event EventHandler<GeoShareEventArgs> GeoShareLocationsChanged;

        private async void StartTimerAsync()
        {
            while (await timer.WaitForNextTickAsync())
            {
                if (!Config.Instance.GeoShare.IsEnabled)
                {
                    continue;
                }
                try
                {
                    var locations = await httpService.GetAsync<IList<UserLocationDto>>(Config.Instance.GeoShare.Server + HttpService.Url_LatestLocations);
                    GeoShareLocationsChanged?.Invoke(this, new GeoShareEventArgs()
                    {
                        Locations = locations
                    });
                    Overlay.Graphics.Clear();
                    foreach (var loc in locations
#if !DEBUG
                        .Where(p=>p.UserName!=Config.Instance.GeoShare.UserName)
#endif
                        )
                    {
                        var mapLocation = new MapPoint(loc.Location.Longitude, loc.Location.Latitude, SpatialReferences.Wgs84);
#if DEBUG
                        loc.Location.Accuracy = 20;
#endif
                        if (loc.Location.Accuracy > 0)
                        {
                            var buffer = GeometryEngine.BufferGeodetic(mapLocation, loc.Location.Accuracy, LinearUnits.Meters);
                            Graphic accuracyGraphic = new Graphic(buffer);
                            accuracyGraphic.Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid,
                                System.Drawing.Color.FromArgb(64, 0, 0x7a,0xc2), null);
                            Overlay.Graphics.Add(accuracyGraphic);
                        }

                        Graphic graphic = new Graphic(mapLocation);
                        graphic.Attributes.Add(nameof(loc.UserName), loc.UserName);
                        graphic.Attributes.Add(nameof(loc.Location.Accuracy), loc.Location.Accuracy);
                        graphic.Attributes.Add(nameof(loc.Location.Altitude), loc.Location.Altitude);
                        graphic.Attributes.Add(nameof(loc.Location.Time), loc.Location.Time);
                        Overlay.Graphics.Add(graphic);
                    }
                    if (locationDisplay.MapLocation != null &&
                        (double.IsNaN(locationDisplay.Location.HorizontalAccuracy)
                        || locationDisplay.Location.HorizontalAccuracy < 100))
                    {
                        var mapLocation = locationDisplay.MapLocation.ToWgs84();
                        await httpService.PostAsync(Config.Instance.GeoShare.Server + HttpService.Url_ReportLocation, new SharedLocationEntity()
                        {
                            Longitude = mapLocation.X,
                            Latitude = mapLocation.Y,
                            Altitude = mapLocation.Z,
                            Accuracy = locationDisplay.Location.HorizontalAccuracy,
                        });

                    }

                }
                catch (Exception ex)
                {
                    GeoShareLocationsChanged?.Invoke(this, new GeoShareEventArgs()
                    {
                        HasException = true,
                        Exception = ex
                    });
                }
            }
        }
    }

    public class GeoShareEventArgs : EventArgs
    {
        public bool HasException { get; set; }
        public Exception Exception { get; set; }
        public IList<UserLocationDto> Locations { get; set; }
    }
}
