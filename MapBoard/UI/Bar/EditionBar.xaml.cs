using FzLib.Extension;
using MapBoard.Main.UI.Map;
using MapBoard.Main.UI.Model;
using System.Windows;
using System.Windows.Controls;
using static MapBoard.Main.UI.Map.EditorHelper;

namespace MapBoard.Main.UI.Bar
{
    /// <summary>
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class EditionBar : BarBase
    {
        public EditionBar()
        {
            InitializeComponent();
        }

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
            MapView.SketchEditor.SelectedVertexChanged += SketchEditorSelectedVertexChanged;
        }

        public override FeatureAttributeCollection Attributes => MapView.Editor.Attributes;

        private void SketchEditorSelectedVertexChanged(object sender, Esri.ArcGISRuntime.UI.VertexChangedEventArgs e)
        {
            btnDeleteSelectedVertex.IsEnabled = MapView.SketchEditor.SelectedVertex != null;
        }

        public override double ExpandDistance => 28;

        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;


        private void BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTask.Draw)
            {
                switch (MapView.Editor.Mode)
                {
                    case EditMode.Creat:
                        Title = "正在绘制";
                        //SetBarHeight(false);
                        break;

                    case EditMode.Edit:
                        Title = "正在编辑";
                        //SetBarHeight(false);
                        break;

                    //case EditMode.GetLine:
                    //    Title = "正在切割（请绘制用于切割的线段）";
                    //    //SetBarHeight(true);
                    //    break;

                    default:
                        return;
                }
                this.Notify(nameof(MapView));
                Expand();
            }
            else
            {
                Collapse();
            }
        }

        private string title = "正在编辑";

        public string Title
        {
            get => title;
            set
            {
                title = value;
                this.Notify(nameof(Title));
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

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                (sender as TextBox).GetBindingExpression(TextBox.TextProperty)
                      .UpdateSource();//由于要失去焦点才会更新数据，因此按回车以后要手动强制更新
                MapView.Editor.StopAndSave();
            }
        }
    }
}