using FzLib.Extension;
using FzLib.WPF.Dialog;

using MapBoard.IO;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static FzLib.Media.Converter;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using MapBoard.Mapping.Model;
using System.Windows.Input;

namespace MapBoard.UI
{
    /// <summary>
    /// RendererSettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LayerListPanel : UserControlBase
    {
        private LayerListPanelHelper layerListHelper;
        public LayerListPanel()
        {
            InitializeComponent();
        }
        private bool changingSelection = false;

        public MainMapView MapView { get; set; }
        public MapLayerCollection Layers => MapView.Layers;

        public void Initialize(MainMapView mapView)
        {
            MapView = mapView;
            //设置图层列表的数据源并初始化选中的图层
            dataGrid.ItemsSource = mapView.Layers;
            dataGrid.SelectedItem = mapView.Layers.Selected;
            Layers.PropertyChanged += (p1, p2) =>
            {
                if (p2.PropertyName == nameof(mapView.Layers.Selected) && !changingSelection)
                {
                    dataGrid.SelectedItem = mapView.Layers.Selected;
                }
            };
            //初始化图层列表相关操作
            layerListHelper = new LayerListPanelHelper(dataGrid, Window.GetWindow(this) as MainWindow, mapView);
        }

        public void JudgeControlsEnable()
        {
            if (MapView.Selection.SelectedFeatures.Count > 0)
            {
                dataGrid.IsEnabled = false;
            }
            else
            {
                dataGrid.IsEnabled = true;
            }
        }   /// <summary>
            /// 图层项右键，用于显示菜单
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
        private void ListItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            layerListHelper.ShowContextMenu();
        }

        /// <summary>
        /// 图层列表右键按下时，就使列表项被选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lvw_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            layerListHelper.RightButtonClickToSelect(e);
        }

        /// <summary>
        /// 选中的图层变化事件。图层列表选中项不使用绑定。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedLayerChanged(object sender, SelectionChangedEventArgs e)
        {
            changingSelection = true;
            if (dataGrid.SelectedItems.Count == 1)
            {
                Layers.Selected = dataGrid.SelectedItem as MapLayerInfo;
            }
            else
            {
                Layers.Selected = null;
            }
            changingSelection = false;
        }
        public event SelectionChangedEventHandler SelectionChanged
        {
            add
            {
                dataGrid.SelectionChanged += value;
            }
            remove
            {
                dataGrid.SelectionChanged -= value;
            }
        }
    }
}