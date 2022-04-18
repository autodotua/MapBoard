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
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class BufferDialog : CommonDialog
    {
        public BufferDialog(MapLayerCollection layers)
        {
            Layers = layers;
            PolygonLayers = Layers
                .EditableLayers
                .Where(p => p.GeometryType is Esri.ArcGISRuntime.Geometry.GeometryType.Polygon)
                .Cast<IEditableLayerInfo>()
                .ToList();
            InitializeComponent();
        }

        public double[] Distances { get; private set; }

        public ObservableCollection<ValueTypeWrapper<double>> DistanceWrappers { get; set; }
            = new ObservableCollection<ValueTypeWrapper<double>>(
                Config.Instance.BufferDistances.Select(p => new ValueTypeWrapper<double>(p)));

        public MapLayerCollection Layers { get; }
        public string Message { get; set; }
        public List<IEditableLayerInfo> PolygonLayers { get; }
        public IEditableLayerInfo TargetLayer { get; set; }
        public bool ToNewLayer { get; set; } = false;
        public bool Union { get; set; } = true;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void AddDistanceButton_Click(object sender, RoutedEventArgs e)
        {
            DistanceWrappers.Add(new ValueTypeWrapper<double>(double.NaN));
        }
    }

    public class ValueTypeWrapper<T> where T : struct
    {
        public ValueTypeWrapper()
        {
        }

        public ValueTypeWrapper(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}