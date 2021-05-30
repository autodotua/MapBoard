using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib.Extension;
using MapBoard.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapBoard.Main.UI.Component
{
    /// <summary>
    /// MapViewInfoPanel.xaml 的交互逻辑
    /// </summary>
    public partial class MapViewInfoPanel : UserControlBase
    {
        private object lockObj = new object();
        private bool canUpdate = true;
        private Timer timer;

        public MapViewInfoPanel()
        {
            InitializeComponent();

            //用于限制最多100毫秒更新一次
            timer = new Timer(new TimerCallback(p =>
             {
                 lock (lockObj)
                 {
                     canUpdate = true;
                 }
             }), null, 100, 100);
        }

        private MapPoint location;

        public void Update(MapView map, Point? position)
        {
            lock (lockObj)
            {
                if (!canUpdate)
                {
                    return;
                }
                canUpdate = false;
            }
            if (position.HasValue)
            {
                location = map.ScreenToLocation(position.Value);
            }
            if (location != null)
            {
                location = GeometryEngine.Project(location, SpatialReferences.Wgs84) as MapPoint;
                Latitude = location.Y.ToString("0.000000");
                Longitude = location.X.ToString("0.000000");
                Scale = (map.UnitsPerPixel * ActualWidth * Math.Cos(Math.PI / 180 * location.Y)).ToString("0.00m");
            }
        }

        private string latitude;

        public string Latitude
        {
            get => latitude;
            private set => this.SetValueAndNotify(ref latitude, value, nameof(Latitude));
        }

        private string longitude;

        public string Longitude
        {
            get => longitude;
            private set => this.SetValueAndNotify(ref longitude, value, nameof(Longitude));
        }

        private string scale;

        public string Scale
        {
            get => scale;
            private set => this.SetValueAndNotify(ref scale, value, nameof(Scale));
        }
    }
}