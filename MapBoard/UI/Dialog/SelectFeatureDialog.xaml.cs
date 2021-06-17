using Esri.ArcGISRuntime.Data;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using MapBoard.Main.UI.Bar;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System;
using System.Diagnostics;
using MapBoard.Main.UI.Map.Model;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Linq;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectFeatureDialog : Common.DialogWindowBase
    {
        public ObservableCollection<FeatureSelectionInfo> SelectedFeatures { get; } = new ObservableCollection<FeatureSelectionInfo>();

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

        public SelectFeatureDialog(Window owner, SelectionHelper selection, MapLayerCollection layers) : base(owner)
        {
            var mainWindow = Application.Current.MainWindow;
            Owner = mainWindow;
            Selection = selection;
            Layers = layers;
            WindowStartupLocation = WindowStartupLocation.Manual;
            InitializeComponent();
            Selection.CollectionChanged += SelectedFeaturesChanged;
            SelectedFeaturesChanged(null, null);
        }

        private async void SelectedFeaturesChanged(object sender, EventArgs e)
        {
            if (Selection.SelectedFeatures.Count < 2)
            {
                return;
            }
            SelectedFeatures.Clear();
            int index = 0;
            List<FeatureSelectionInfo> featureSelections = null;
            await Task.Run(() =>
            {
                featureSelections = Selection.SelectedFeatures
                .Select(p => new FeatureSelectionInfo(Layers.Selected, p, ++index))
                .ToList();
            });
            featureSelections.ForEach(p => SelectedFeatures.Add(p));
        }

        public void ResetLocation()
        {
            if (IsClosed)
            {
                return;
            }
            var mainWindow = Application.Current.MainWindow;
            double left = mainWindow.Left;
            double top = mainWindow.Top;
            if (mainWindow.WindowState == WindowState.Maximized)
            {
                left = top = 0;
            }
            mainWindow.IsVisibleChanged += (p1, p2) =>
            {
                if (p2.NewValue.Equals(false))
                {
                    Visibility = Visibility.Collapsed;
                }
            };

            Left = left + mainWindow.ActualWidth - ActualWidth;
            Top = top + mainWindow.ActualHeight - Height;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetLocation();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            ResetLocation();
            base.OnContentRendered(e);
            var mainWindow = Application.Current.MainWindow;

            mainWindow.Focus();
        }

        public class FeatureSelectionInfo
        {
            public FeatureSelectionInfo(MapLayerInfo layer, Feature feature, int index)
            {
                Feature = feature;
                Index = index;
                Attributes = FeatureAttributes.FromFeature(layer, feature);
            }

            public Feature Feature { get; }
            public int Index { get; }
            public FeatureAttributes Attributes { get; set; }
        }
    }
}