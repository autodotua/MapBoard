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

namespace MapBoard.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync()
        {
            await map.LoadAsync();
            layerList.Initialize();
        }

        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            await InitializeAsync();
        }

        private void CloseLayerPanelButton_Click(object sender, EventArgs e)
        {
            grdLayer.TranslateTo(-300, 0);
            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                grdSide.TranslateTo(0, 0);
            }
        }

        private void LayerButton_Click(object sender, EventArgs e)
        {
            grdLayer.TranslateTo(0, 0);
            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                grdSide.TranslateTo(100, 0);
            }
        }


        private async void ZoomInButton_Click(object sender, EventArgs e)
        {
            var map = MainMapView.Current;
            await map.SetViewpointScaleAsync(map.MapScale / 3);
        }

        private async void ZoomOutButton_Click(object sender, EventArgs e)
        {
            var map = MainMapView.Current;
            await map.SetViewpointScaleAsync(map.MapScale * 3);
        }
    }

}
