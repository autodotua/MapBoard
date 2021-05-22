using Esri.ArcGISRuntime.Data;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using MapBoard.Main.UI.OperationBar;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectFeatureDialog : Common.Dialog.DialogWindowBase
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
                    ArcMapView.Instance.Selection.Select(value.Feature, true);
                }
            }
        }

        public SelectFeatureDialog()
        {
            var mainWindow = Application.Current.MainWindow;
            Owner = mainWindow;
            WindowStartupLocation = WindowStartupLocation.Manual;
            InitializeComponent();
            ArcMapView.Instance.Selection.SelectedFeatures.CollectionChanged += SelectedFeaturesChanged;
            SelectedFeaturesChanged(null, null);
        }

        private void SelectedFeaturesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ArcMapView.Instance.Selection.SelectedFeatures.Count < 2)
            {
                return;
            }
            SelectedFeatures.Clear();
            int index = 0;
            foreach (var feature in ArcMapView.Instance.Selection.SelectedFeatures)
            {
                SelectedFeatures.Add(new FeatureSelectionInfo(MapLayerCollection.Instance.Selected, feature, ++index));
            }
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

            Left = left + mainWindow.ActualWidth - ActualWidth;
            Top = top + OperationBarBase.DefaultBarHeight + 2/*bar的高度*/ + SystemParameters.WindowCaptionHeight * VisualTreeHelper.GetDpi(mainWindow).DpiScaleY;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;
            ResetLocation();
            mainWindow.Focus();
        }

        public class FeatureSelectionInfo
        {
            public FeatureSelectionInfo(LayerInfo layer, Feature feature, int index)
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