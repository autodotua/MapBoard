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

        private bool isBusy = false;
        private DateTime lastReportTime = DateTime.MinValue;

        public GeoShareService(GraphicsOverlay overlay)
        {
            Overlay = overlay;
        }

        public event EventHandler<ExceptionEventArgs> GeoShareExceptionThrow;

        public GraphicsOverlay Overlay { get; }

        public async Task ReportLocationAsync(Location location)
        {
            if (!Config.Instance.GeoShare.IsEnabled || !Config.Instance.GeoShare.ShareLocation)
            {
                return;
            }
            if ((DateTime.Now - lastReportTime).Seconds < 10)
            {
                return;
            }
            try
            {
                lastReportTime = DateTime.Now;
                await httpService.PostAsync(Config.Instance.GeoShare.Server + HttpService.Url_ReportLocation, new SharedLocationEntity()
                {
                    Longitude = location.Longitude,
                    Latitude = location.Latitude,
                    Altitude = location.Altitude ?? 0,
                    Accuracy = location.Accuracy ?? 0
                });
                lastReportTime = DateTime.Now;
            }
            catch (HttpRequestException ex)
            {
                //登陆存在问题
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    try
                    {
                        await LoginAsync();
                    }
                    catch (Exception ex2)
                    {
                        GeoShareExceptionThrow?.Invoke(this, new ExceptionEventArgs(ex));
                        Config.Instance.GeoShare.IsEnabled = false;
                    }
                }
                else
                {
                    GeoShareExceptionThrow?.Invoke(this, new ExceptionEventArgs(ex));
                }
            }
            catch (Exception ex)
            {
                GeoShareExceptionThrow?.Invoke(this, new ExceptionEventArgs(ex));
            }
        }

        public async void Start()
        {
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
            var timer = App.Current.Dispatcher.CreateTimer();
            timer.Tick += Timer_Tick;
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
        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (isBusy || !Config.Instance.GeoShare.IsEnabled)
            {
                return;
            }
            isBusy = true;
            try
            {
                var locations = await httpService.GetAsync<IList<UserLocationDto>>(Config.Instance.GeoShare.Server + HttpService.Url_LatestLocations);
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
                        Graphic accuracyGraphic = new Graphic(buffer)
                        {
                            ZIndex = 10,
                            Symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(64, 0, 0x7a, 0xc2), null)
                        };
                        Overlay.Graphics.Add(accuracyGraphic);
                    }

                    Dictionary<string, object> attrs = new Dictionary<string, object>()
                    {
                        [nameof(loc.UserName)] = loc.UserName,
                        [nameof(loc.Location.Accuracy)] = loc.Location.Accuracy,
                        [nameof(loc.Location.Altitude)] = loc.Location.Altitude,
                        [nameof(loc.Location.Time)] = loc.Location.Time
                    };
                    Graphic graphic = new Graphic(mapLocation, attrs) { ZIndex = 100 };
                    Overlay.Graphics.Add(graphic);
                }

            }
            catch (HttpRequestException ex)
            {
                //登陆存在问题
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    try
                    {
                        await LoginAsync();
                    }
                    catch (Exception ex2)
                    {
                        GeoShareExceptionThrow?.Invoke(this, new ExceptionEventArgs(ex));
                        Config.Instance.GeoShare.IsEnabled = false;
                    }
                }
                else
                {
                    GeoShareExceptionThrow?.Invoke(this, new ExceptionEventArgs(ex));
                }
            }
            catch (Exception ex)
            {
                GeoShareExceptionThrow?.Invoke(this, new ExceptionEventArgs(ex));
            }
            finally
            {
                isBusy = false;
            }
        }
    }
}
