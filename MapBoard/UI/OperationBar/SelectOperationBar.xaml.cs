using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using FzLib.Basic;
using MapBoard.Style;
using MapBoard.UI.BoardOperation;
using MapBoard.UI.Dialog;
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

namespace MapBoard.UI.OperationBar
{
    /// <summary>
    /// EditOperationBar.xaml 的交互逻辑
    /// </summary>
    public partial class SelectOperationBar : OperationBarBase
    {
        public SelectOperationBar()
        {
            InitializeComponent();
            BoardTaskManager.BoardTaskChanged += BoardTaskChanged;
        }

        private void SelectedFeaturesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            btnRedraw.IsEnabled = MapView.Selection.SelectedFeatures.Count == 1;
            btnCut.IsEnabled = MapView.Selection.SelectedFeatures.Count == 1 &&
                (StyleCollection.Instance.Selected.Type == GeometryType.Polygon || StyleCollection.Instance.Selected.Type == GeometryType.Polyline);
            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count.ToString()}个图形");
            if (StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polyline)//线
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                sb.Append("，长度：" + Number.MeterToFitString(length));
            }
            else if (StyleCollection.Instance.Selected.Table.GeometryType == GeometryType.Polyline)//面
            {
                double length = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.LengthGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                double area = MapView.Selection.SelectedFeatures.Sum(p => GeometryEngine.AreaGeodetic(p.Geometry, null, GeodeticCurveType.NormalSection));
                sb.Append("，周长：" + Number.MeterToFitString(length));
                sb.Append("，面积：" + Number.SquareMeterToFitString(area));
            }
            Message = sb.ToString();
        }

        private void BoardTaskChanged(object sender, BoardTaskManager.BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTaskManager.BoardTask.Select)
            {
                MapView.Selection.SelectedFeatures.CollectionChanged += SelectedFeaturesChanged;
                SelectedFeaturesChanged(null, null);
                Show();
            }
            else
            {
                MapView.Selection.SelectedFeatures.CollectionChanged -= SelectedFeaturesChanged;
                Hide();
            }
        }

        public ArcMapView MapView => ArcMapView.Instance;


        private string message = "正在编辑";
        public string Message
        {
            get => message;
            set
            {
                message = value;
                Notify(nameof(Message));
            }
        }

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            await MapView.Editing.DeleteSelectedFeatures();
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            SelectStyleDialog dialog = new SelectStyleDialog();
            if (dialog.ShowDialog() == true)
            {
                StyleCollection.Instance.Selected.LayerVisible = false;
                ObservableCollection<Feature> features = MapView.Selection.SelectedFeatures;
                ShapefileFeatureTable targetTable = dialog.SelectedStyle.Table;
                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                await MapView.Selection.StopFrameSelect(false);
                dialog.SelectedStyle.UpdateFeatureCount();
                StyleCollection.Instance.Selected = dialog.SelectedStyle;
            }
        }
        private void CutButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Editing.StartEdit(EditHelper.EditMode.Cut);
        }
        private void ReDrawButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Editing.StartEdit(EditHelper.EditMode.Draw);
        }

        private async void CancelButtonClick(object sender, RoutedEventArgs e)
        {
          await  MapView.Selection.StopFrameSelect(false);
        }
    }
}
