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

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectFeatureDialog : DialogWindowBase
    {
        public const int MaxCount = 100;
        private ObservableCollection<FeatureSelectionInfo> selectedFeatures;

        public ObservableCollection<FeatureSelectionInfo> SelectedFeatures
        {
            get => selectedFeatures;
            set => this.SetValueAndNotify(ref selectedFeatures, value, nameof(SelectedFeatures));
        }

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

        private string message;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

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
            await Task.Run(() =>
            {
                featureSelections = Selection.SelectedFeatures
                    .Take(MaxCount)
                    .Select(p => new FeatureSelectionInfo(Layers.Selected, p, ++index))
                    .ToList();
                SelectedFeatures = new ObservableCollection<FeatureSelectionInfo>(featureSelections);
            });
        }

        public void ResetLocation()
        {
            if (IsClosed)
            {
                return;
            }
            double left = Owner.Left;
            double top = Owner.Top;
            if (Owner.WindowState == WindowState.Maximized)
            {
                left = top = 0;
            }
            Owner.IsVisibleChanged += (p1, p2) =>
            {
                if (p2.NewValue.Equals(false))
                {
                    Visibility = Visibility.Collapsed;
                }
            };

            Left = left + Owner.ActualWidth - ActualWidth;
            Top = top + Owner.ActualHeight - Height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetLocation();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            ResetLocation();
            base.OnContentRendered(e);
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