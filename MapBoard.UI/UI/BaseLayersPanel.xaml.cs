using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using FzLib;
using MapBoard.Mapping;
using MapBoard.Model;
using MapBoard.UI;
using MapBoard.UI.Component;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapBoard.UI
{
    /// <summary>
    /// 底图图层面板
    /// </summary>
    public partial class BaseLayersPanel : UserControlBase
    {
        /// <summary>
        /// 简单视图模式属性
        /// </summary>
        public static DependencyProperty SimpleModeProperty = DependencyProperty.Register(nameof(SimpleMode),
                                                                                          typeof(bool),
                                                                                          typeof(BaseLayersPanel),
                                                                                          new PropertyMetadata(false, new PropertyChangedCallback((d, e) => (d as BaseLayersPanel).SetBaseLayersData())));

        public BaseLayersPanel()
        {
            SetBaseLayersData();
            InitializeComponent();
        }

        /// <summary>
        /// 所有底图图层
        /// </summary>
        public ObservableCollection<BaseLayerInfo> BaseLayers { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; }

        public BaseLayerInfo SelectedItem
        {
            get => grd.SelectedItem as BaseLayerInfo;
            set => grd.SelectedItem = value;
        }

        /// <summary>
        /// 选择的底图
        /// </summary>
        public List<BaseLayerInfo> SelectedItems
        {
            get => grd.SelectedItems.Cast<BaseLayerInfo>().ToList();
        }

        /// <summary>
        /// 简单视图模式。启用时，仅显示名称、透明度、可视开关，不可拖动。
        /// </summary>
        public bool SimpleMode
        {
            get => (bool)GetValue(SimpleModeProperty);
            set => SetValue(SimpleModeProperty, value);
        }

        /// <summary>
        /// 将指定项目移动到视图
        /// </summary>
        /// <param name="item"></param>
        public void ScrollIntoView(BaseLayerInfo item)
        {
            grd.ScrollIntoView(item);
        }

        /// <summary>
        /// 单击图层高级设置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdvancedLayerPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage = null;
        }

        /// <summary>
        /// 单击应用图层高级属性按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyLayerPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            var baseLayer = (sender as FrameworkElement).DataContext as BaseLayerInfo;
            try
            {
                ChangeLayerProperty(baseLayer, (arc, b) => b.ApplyBaseLayerStyles(arc));
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// 切换底图可见按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseLayerVisibleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var baseLayer = (sender as FrameworkElement).DataContext as BaseLayerInfo;
            ChangeLayerProperty(baseLayer, (arc, b) => arc.IsVisible = b.Visible);

        }

        /// <summary>
        /// 改变图层属性
        /// </summary>
        /// <param name="baseLayer"></param>
        /// <param name="layerAction"></param>
        private void ChangeLayerProperty(BaseLayerInfo baseLayer, Action<Layer, BaseLayerInfo> layerAction)
        {
            foreach (var map in MainMapView.Instances.Cast<GeoView>().Concat(BrowseSceneView.Instances))
            {
                Basemap basemap = null;
                if (map is MapView m)
                {
                    basemap = m.Map.Basemap;
                }
                else if (map is SceneView s)
                {
                    basemap = s.Scene.Basemap;
                }
                var arcBaseLayer = basemap.BaseLayers.FirstOrDefault(p => p.Id == baseLayer.TempID.ToString());
                if (arcBaseLayer != null)
                {
                    layerAction(arcBaseLayer, baseLayer);
                }
            }
        }

        /// <summary>
        /// 设置底图数据源
        /// </summary>
        private void SetBaseLayersData()
        {
            BaseLayers = new ObservableCollection<BaseLayerInfo>(SimpleMode ?
                Config.Instance.BaseLayers.Where(p => p.Enable) : Config.Instance.BaseLayers);
        }

        /// <summary>
        /// 透明度滑动条滑动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var baseLayer = (sender as FrameworkElement).DataContext as BaseLayerInfo;
            ChangeLayerProperty(baseLayer, (arc, b) => arc.Opacity = b.Opacity);
        }
    }
}
