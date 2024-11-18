using Esri.ArcGISRuntime.Data;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 缓冲区对话框
    /// </summary>
    public partial class BufferDialog : CommonDialog
    {
        public BufferDialog(MapLayerCollection layers)
        {
            Layers = layers;
            //加载目标图层
            PolygonLayers = Layers
                .EditableLayers
                .Where(p => p.GeometryType is Esri.ArcGISRuntime.Geometry.GeometryType.Polygon)
                .Cast<IMapLayerInfo>()
                .ToList();
            InitializeComponent();
        }

        /// <summary>
        /// 距离
        /// </summary>
        public double[] Distances { get; private set; }

        /// <summary>
        /// 距离值，为了方便绑定，修改为引用类型
        /// </summary>
        public ObservableCollection<ValueTypeWrapper<double>> DistanceWrappers { get; set; }
            = new ObservableCollection<ValueTypeWrapper<double>>(
                Config.Instance.BufferDistances.Select(p => new ValueTypeWrapper<double>(p)));

        /// <summary>
        /// 所有图层
        /// </summary>
        public MapLayerCollection Layers { get; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 所有多边形图层
        /// </summary>
        public List<IMapLayerInfo> PolygonLayers { get; }

        /// <summary>
        /// 目标图层
        /// </summary>
        public IMapLayerInfo TargetLayer { get; set; }

        /// <summary>
        /// 是否导出到新图层
        /// </summary>
        public bool ToNewLayer { get; set; } = false;

        /// <summary>
        /// 是否合并
        /// </summary>
        public bool Union { get; set; } = true;

        /// <summary>
        /// 单击增加距离值（环）按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddDistanceButton_Click(object sender, RoutedEventArgs e)
        {
            DistanceWrappers.Add(new ValueTypeWrapper<double>(double.NaN));
        }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            var distances = DistanceWrappers
                .Where(p => !double.IsNaN(p.Value))
                .Select(p => p.Value)
                .OrderBy(p => p)
                .ToList();
            if (distances.Count == 0 || distances.Any(p => p <= 0))
            {
                Message = "距离超出范围";
                args.Cancel = true;
            }
            else if (Enumerable.Range(0, distances.Count - 1).Any(i => distances[i] == distances[i + 1]))
            {
                Message = "存在相邻相同的距离";
                args.Cancel = true;
            }
            else if (ToNewLayer == false && TargetLayer == null)
            {
                Message = "未指定目标图层";
                args.Cancel = true;
            }
            DistanceWrappers = new ObservableCollection<ValueTypeWrapper<double>>(
                distances.Select(p => new ValueTypeWrapper<double>(p)));
            Config.Instance.BufferDistances = distances;
            Distances = distances.ToArray();
        }
    }
}