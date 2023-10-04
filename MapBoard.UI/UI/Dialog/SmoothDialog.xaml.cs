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
    /// 图形平滑对话框
    /// </summary>
    public partial class SmoothDialog : CommonDialog
    {
        public SmoothDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 每个段新增的点数量
        /// </summary>
        public int PointsPerSegment { get; set; } = 10;

        /// <summary>
        /// 平滑级别
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// 平滑后简化最大垂距
        /// </summary>
        public double MaxDeviation { get; set; } = 1;

        /// <summary>
        /// 是否平滑后简化
        /// </summary>
        public bool Simplify { get; set; } = true;

        /// <summary>
        /// 是否删除原有图形
        /// </summary>
        public bool DeleteOldFeature { get; set; } = false;

        /// <summary>
        /// 需要平滑的最小角度
        /// </summary>
        public double MinSmoothAngle { get; set; } = 45d;

        /// <summary>
        /// 单击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
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