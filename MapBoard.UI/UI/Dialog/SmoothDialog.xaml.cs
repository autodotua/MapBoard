using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SmoothDialog : CommonDialog
    {
        public SmoothDialog()
        {
            InitializeComponent();
        }

        public string Message { get; set; }
        public int PointsPerSegment { get; set; } = 10;
        public int Level { get; set; } = 1;
        public double MaxDeviation { get; set; } = 1;
        public bool Simplify { get; set; } = true;

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            if (PointsPerSegment <= 1)
            {
                Message = "点数量过少";
                args.Cancel = true;
            }
            if (Simplify && MaxDeviation <= 0)
            {
                Message = "最小垂距不可小于等于0";
                args.Cancel = true;
            }
        }
    }
}