using Esri.ArcGISRuntime.Data;
using MapBoard.Mapping;
using MapBoard.UI.Bar;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Linq;
using MapBoard.Mapping.Model;
using FzLib;
using System.Windows.Controls;
using System.Windows.Data;
using MapBoard.Model;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 选择要素吸附小窗
    /// </summary>
    public partial class SelectFeatureDialog : RightBottomFloatDialogBase
    {
        /// <summary>
        /// 最大显示的要素数量
        /// </summary>
        public const int MaxCount = 100;

        /// <summary>
        /// 选择的要素
        /// </summary>
        private FeatureSelectionInfo selected;


        public SelectFeatureDialog(Window owner, MainMapView mapView, MapLayerCollection layers) : base(owner)
        {
            Selection = mapView.Selection;
            MapView = mapView;
            Layers = layers;
            WindowStartupLocation = WindowStartupLocation.Manual;
            InitializeComponent();
            mapView.Selection.CollectionChanged += SelectedFeaturesChanged;
            mapView.BoardTaskChanged += MapView_BoardTaskChanged;

            SelectedFeaturesChanged(null, null);
        }

        /// <summary>
        /// 所有图层
        /// </summary>
        public MapLayerCollection Layers { get; }

        /// <summary>
        /// 地图
        /// </summary>
        public MainMapView MapView { get; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 在本窗口中选择的要素
        /// </summary>
        public FeatureSelectionInfo Selected
        {
            get => selected;
            set
            {
                selected = value;
                if (value != null)
                {
                    Selection.Select(value.Feature, true);
                }
            }
        }

        /// <summary>
        /// 所有选择的要素
        /// </summary>
        public ObservableCollection<FeatureSelectionInfo> SelectedFeatures { get; set; }

        /// <summary>
        /// 选择帮助类
        /// </summary>
        public SelectionHelper Selection { get; }
        private async void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var feature = (e.Source as FrameworkElement).DataContext as FeatureSelectionInfo;
            if (feature != null)
            {
                await MapView.ZoomToGeometryAsync(feature.Feature.Geometry);
            }
        }

        /// <summary>
        /// 退出选择要素状态或关闭窗口后，关闭本窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MapView_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask is not BoardTask.Select && !IsClosed)
            {
                Close();
            }
        }

        /// <summary>
        /// 选择的要素如果发生改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SelectedFeaturesChanged(object sender, EventArgs e)
        {
            int count = Selection.SelectedFeatures.Count;
            if (count < 2)
            {
                return;
            }
            if (count > MaxCount)
            {
                Message = $"共{count}条，显示{MaxCount}条";
            }
            else
            {
                Message = $"共{count}条";
            }
            int index = 0;
            List<FeatureSelectionInfo> featureSelections = null;
            GridView view = lvw.View as GridView;
            view.Columns.Clear();
            view.Columns.Add(new GridViewColumn() { DisplayMemberBinding = new Binding("Index") });

            //处理每个要素的属性
            await Task.Run(() =>
            {
                featureSelections = Selection.SelectedFeatures
                    .Take(MaxCount)
                    .Select(p => new FeatureSelectionInfo(Layers.Selected, p, ++index))
                    .ToList();
                SelectedFeatures = new ObservableCollection<FeatureSelectionInfo>(featureSelections);
            });

            //设置列
            var attr = SelectedFeatures.First().Attributes;
            for (int i = 0; i < attr.Attributes.Count; i++)
            {
                var field = attr.Attributes[i];
                view.Columns.Add(new GridViewColumn()
                {
                    Header = field.DisplayName,
                    DisplayMemberBinding = new Binding($"{nameof(FeatureSelectionInfo.Attributes)}.{nameof(FeatureAttributeCollection.Attributes)}[{i}]")
                });
            }
        }

    }
}