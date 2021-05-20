using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
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

        private void BoardTaskChanged(object sender, BoardTaskManager.BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTaskManager.BoardTask.Draw || e.NewTask == BoardTaskManager.BoardTask.Edit)
            {
                if (e.NewTask == BoardTaskManager.BoardTask.Draw)
                {
                    Title = "正在绘制";
                }
                else
                {
                    if (MapView.Editor.Mode == EditMode.Edit)
                    {
                        Title = "正在编辑";
                    }
                    else
                    {
                        Title = "正在切割（请绘制用于切割的线段）";
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