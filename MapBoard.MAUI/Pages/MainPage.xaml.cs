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

namespace MapBoard.Pages
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            map.Map = new Esri.ArcGISRuntime.Mapping.Map();

            map.Map.Basemap = new Esri.ArcGISRuntime.Mapping.Basemap(new WebTiledLayer(""));
            map.Margin = new Thickness(-72);
            map.LoadAsync();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {


            //SemanticScreenReader.Announce(CounterBtn.Text);
        }


    }

}
