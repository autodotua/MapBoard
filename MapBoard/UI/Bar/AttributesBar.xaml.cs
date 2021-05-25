using FzLib.Extension;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System.Windows;
using System.Windows.Controls;
using static MapBoard.Main.UI.Map.EditorHelper;

namespace MapBoard.Main.UI.Bar
{
    /// <summary>
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class AttributesBar : BarBase
    {
        public AttributesBar()
        {
            InitializeComponent();
            Width = ExpandDistance;
        }

        public override void Initialize()
        {
            MapView.BoardTaskChanged += (s, e) => ExpandOrCollapse();
            MapView.Selection.SelectedFeatures.CollectionChanged += (s, e) => ExpandOrCollapse();
        }

        private FeatureAttributes attributes;
        public override FeatureAttributes Attributes => attributes;

        public override double ExpandDistance => 240;

        protected override ExpandDirection ExpandDirection => ExpandDirection.Left;

        private void ExpandOrCollapse()
        {
            switch (MapView.CurrentTask)
            {
                case BoardTask.Draw
                            when MapView.Editor.Mode == EditMode.Creat
                            || MapView.Editor.Mode == EditMode.Edit:
                    attributes = MapView.Editor.Attributes;
                    dataGrid.Columns[1].IsReadOnly = false;
                    break;

                case BoardTask.Select
                            when MapView.Layers.Selected != null
                            && MapView.Selection.SelectedFeatures.Count == 1:
                    attributes = FeatureAttributes.FromFeature(MapView.Layers.Selected, MapView.Selection.SelectedFeatures[0]);
                    dataGrid.Columns[1].IsReadOnly = true;
                    break;

                default:
                    Collapse();
                    return;
            }
            this.Notify(nameof(Attributes));
            Expand();
        }

        private void DataGrid_Selected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);
            }
        }
    }
}