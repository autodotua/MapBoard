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
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectFeatureDialog : RightBottomFloatDialogBase
    {
        public const int MaxCount = 100;
        public ObservableCollection<FeatureSelectionInfo> SelectedFeatures { get; set; }

        private FeatureSelectionInfo selected;

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

        public SelectionHelper Selection { get; }
        public MapLayerCollection Layers { get; }

        public SelectFeatureDialog(Window owner, MainMapView mapView, MapLayerCollection layers) : base(owner)
        {
            Selection = mapView.Selection;
            Layers = layers;
            WindowStartupLocation = WindowStartupLocation.Manual;
            InitializeComponent();
            mapView.Selection.CollectionChanged += SelectedFeaturesChanged;
            mapView.BoardTaskChanged += MapView_BoardTaskChanged;

            SelectedFeaturesChanged(null, null);
        }

        private void MapView_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask is not BoardTask.Select && !IsClosed)
            {
                Close();
            }
        }

        public string Message { get; set; }

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




        public class FeatureSelectionInfo
        {
            public FeatureSelectionInfo(IMapLayerInfo layer, Feature feature, int index)
            {
                Feature = feature;
                Index = index;
                Attributes = FeatureAttributeCollection.FromFeature(layer, feature);
            }

            public Feature Feature { get; }
            public int Index { get; }
            public FeatureAttributeCollection Attributes { get; set; }
        }
    }
}