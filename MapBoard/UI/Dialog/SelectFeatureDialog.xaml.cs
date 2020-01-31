using Esri.ArcGISRuntime.Data;
using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common.Dialog;
using MapBoard.Common.Resource;
using MapBoard.Main.Layer;
using MapBoard.Main.UI.Map;
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
using System.Windows.Shapes;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectFeatureDialog : Common.Dialog.DialogWindowBase
    {

        public ObservableCollection<FeatherSelectionInfo> SelectedFeatures { get; } = new ObservableCollection<FeatherSelectionInfo>();

        private FeatherSelectionInfo selected;
        public FeatherSelectionInfo Selected
        {
            get => selected;
            set
            {
                selected = value;
                if(value!=null)
                {
                    ArcMapView.Instance.Selection.Select(value.Feature,true);
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
            if(ArcMapView.Instance.Selection.SelectedFeatures.Count<2)
            {
                return;
            }
            SelectedFeatures.Clear();
            int index = 0;
            foreach (var feature in ArcMapView.Instance.Selection.SelectedFeatures)
            {
                SelectedFeatures.Add(new FeatherSelectionInfo(feature, ++index));
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
            if (mainWindow.WindowState==WindowState.Maximized)
            {
                left = top = 0;
            }

            Left = left + mainWindow.ActualWidth - ActualWidth;
            Top = top + 24/*bar的高度*/ + SystemParameters.WindowCaptionHeight * VisualTreeHelper.GetDpi(mainWindow).DpiScaleY;

        }




        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;
            ResetLocation();
            mainWindow.Focus();
        }



        public class FeatherSelectionInfo
        {
            public FeatherSelectionInfo(Feature feature,int index)
            {
                Feature = feature;
                   Index = index;
                Label = feature.GetAttributeValue(Resource.DisplayFieldName) as string;
                Key = feature.GetAttributeValue(Resource.KeyFieldName) as string;
            }

            public Feature Feature { get; }
            public int Index { get; }
            public string Label { get; }
            public string Key { get; }
        }
    }
}
