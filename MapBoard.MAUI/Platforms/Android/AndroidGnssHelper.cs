using Android.Content;
using Android.Locations;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Platforms.Android
{
    public class AndroidGnssHelper
    {
        private GnssStatusCallback callback;
        private LocationManager locationManager;
        public AndroidGnssHelper(Context context)
        {
            locationManager = (LocationManager)context.GetSystemService(Context.LocationService);
            callback = new GnssStatusCallback(p =>
            {
                LastStatus = p;
                GnssStatusChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public GnssStatusInfo LastStatus { get; private set; }
        public void Start()
        {
            locationManager.RegisterGnssStatusCallback(callback, null);
        }

        public void Stop()
        {
            locationManager.UnregisterGnssStatusCallback(callback);
        }

        public event EventHandler GnssStatusChanged;
    }

    public class GnssStatusCallback : GnssStatus.Callback
    {
        private readonly Action<GnssStatusInfo> update;

        public GnssStatusCallback(Action<GnssStatusInfo> update)
        {
            this.update = update;
        }

        public override void OnSatelliteStatusChanged(GnssStatus status)
        {
            base.OnSatelliteStatusChanged(status);
            update(new GnssStatusInfo()
            {
                Total = status.SatelliteCount,
                Fixed = Enumerable.Range(0, status.SatelliteCount).Where(status.UsedInFix).Count()
            });
        }
    }

}
