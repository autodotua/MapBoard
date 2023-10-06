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
            map.LoadAsync();
        }


    }

}
