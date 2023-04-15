using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using MapBoard.Mapping;
using MapBoard.Model;
using MapBoard.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace MapBoard
{
    /// <summary>
    /// BaseLayersPanel.xaml 的交互逻辑
    /// </summary>
    public partial class BaseLayersPanel : UserControlBase
    {
        public BaseLayersPanel()
        {
            BaseLayers = new ObservableCollection<BaseLayerInfo>(Config.Instance.BaseLayers);
            InitializeComponent();
        }
        /// <summary>
        /// 所有底图图层
        /// </summary>
        public ObservableCollection<BaseLayerInfo> BaseLayers { get; }

        public BaseLayerInfo SelectedItem
        {
            get => grd.SelectedItem as BaseLayerInfo;
            set => grd.SelectedItem = value;
        }

        public List<BaseLayerInfo> SelectedItems
        {
            get => grd.SelectedItems.Cast<BaseLayerInfo>().ToList();
        }

        public void ScrollIntoView(BaseLayerInfo item)
        {
            grd.ScrollIntoView(item);
        }

        /// <summary>
        /// 切换底图可见按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseLayerVisibleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var baseLayer = (sender as FrameworkElement).Tag as BaseLayerInfo;
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
                    arcBaseLayer.IsVisible = baseLayer.Visible;
                }
            }

        }
    }
}
