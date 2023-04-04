using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.WPF.Dialog;
using MapBoard.IO;
using MapBoard.Model;
using MapBoard.UI.Bar;
using MapBoard.UI.Dialog;
using MapBoard.Mapping;
using ModernWpf.Controls;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ModernWpf.FzExtension;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using MapBoard.UI.TileDownloader;
using System.Threading;

namespace MapBoard.UI
{
    /// <summary>
    /// 3D浏览
    /// </summary>
    public partial class BrowseSceneWindow : WindowBase
    {
        public BrowseSceneWindow()
        {
            InitializeComponent();

            mapInfo.Initialize(arcMap);
        }

        public async Task LoadAsync()
        {
            await arcMap.LoadAsync();
        }
    }
}