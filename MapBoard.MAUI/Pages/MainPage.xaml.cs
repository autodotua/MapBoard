using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Maui;
using MapBoard.Mapping;
using MapBoard.Model;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using FzLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Map = Esri.ArcGISRuntime.Mapping.Map;
using Esri.ArcGISRuntime.UI;
using static MapBoard.Util.GeometryUtility;
using FubarDev.FtpServer.FileSystem.DotNet;
using FubarDev.FtpServer;
using MapBoard.Services;
using MapBoard.Views;

namespace MapBoard.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            if (Current != null)
            {
                throw new Exception("仅允许一个实例");
            }
            Current = this;
            InitializeComponent();

            layer.TranslationX = -300;
            layer.WidthRequest = 300;

            track.TranslationX = -300;
            track.WidthRequest = 300;
        }


        private void ContentPage_Loaded(object sender, EventArgs e)
        {
        }

        public static MainPage Current { get; private set; }

        public void OpenPanel<T>()
        {
            if (typeof(T) == typeof(LayerListView))
            {
                layer.TranslateTo(0, 0);
            }
            else if (typeof(T) == typeof(TrackPage))
            {
                track.TranslateTo(0, 0);
            }
        }
        public void ClosePanel<T>()
        {
            if (typeof(T) == typeof(LayerListView))
            {
                layer.TranslateTo(-300, 0);
            }
            else if (typeof(T) == typeof(TrackPage))
            {
                track.TranslateTo(-300, 0);
            }
        }

        private void CloseLayerPanelButton_Click(object sender, EventArgs e)
        {
            ClosePanel<LayerListView>();
        }

        private void TrackButton_Clicked(object sender, EventArgs e)
        {
            OpenPanel<TrackPage>();
        }

        private void FtpButton_Clicked(object sender, EventArgs e)
        {
            OpenPanel<FtpPage>();
        }
    }

}
