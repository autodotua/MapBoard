using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System.Windows;
using static MapBoard.Main.UI.Map.EditorHelper;

namespace MapBoard.Main.UI.OperationBar
{
    /// <summary>
    /// EditOperationBar.xaml 的交互逻辑
    /// </summary>
    public partial class EditOperationBar : OperationBarBase
    {
        public EditOperationBar()
        {
            InitializeComponent();

            BoardTaskManager.BoardTaskChanged += BoardTaskChanged;
            MapView.SketchEditor.SelectedVertexChanged += SketchEditorSelectedVertexChanged;
            //ppp.PlacementTarget = btnAttri;
        }

        public override FeatureAttributes Attributes => ArcMapView.Instance.Editor.Attributes;
        protected override bool CanEdit => true;

        private void SketchEditorSelectedVertexChanged(object sender, Esri.ArcGISRuntime.UI.VertexChangedEventArgs e)
        {
            btnDeleteSelectedVertex.IsEnabled = MapView.SketchEditor.SelectedVertex != null;
        }

        private double barHeight = 56;
        public override double BarHeight => barHeight;

        public void SetBarHeight(bool oneLine)
        {
            if (oneLine)
            {
                grdProperties.Visibility = Visibility.Collapsed;
                barHeight = 28;
                grd.RowDefinitions[2].Height = new GridLength(0);
                grd.RowDefinitions[3].Height = new GridLength(0);
            }
            else
            {
                barHeight = 56;
                grdProperties.Visibility = Visibility.Visible;
                grd.RowDefinitions[2].Height = new GridLength(24);
                grd.RowDefinitions[3].Height = new GridLength(4);
            }
        }

        private void BoardTaskChanged(object sender, BoardTaskManager.BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTaskManager.BoardTask.Draw || e.NewTask == BoardTaskManager.BoardTask.Edit)
            {
                if (e.NewTask == BoardTaskManager.BoardTask.Draw)
                {
                    Title = "正在绘制";
                    SetBarHeight(false);
                }
                else
                {
                    if (MapView.Editor.Mode == EditMode.Edit)
                    {
                        Title = "正在编辑";
                        SetBarHeight(false);
                    }
                    else
                    {
                        Title = "正在切割（请绘制用于切割的线段）";
                        SetBarHeight(true);
                    }
                }
                Notify(nameof(MapView));
                Show();
            }
            else
            {
                Hide();
            }
        }

        public ArcMapView MapView => ArcMapView.Instance;

        private string title = "正在编辑";

        public string Title
        {
            get => title;
            set
            {
                title = value;
                Notify(nameof(Title));
            }
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Editor.StopAndSave();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Editor.Cancel();
        }

        private void RemoveSelectedVertexButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.SketchEditor.RemoveSelectedVertex();
        }
    }
}