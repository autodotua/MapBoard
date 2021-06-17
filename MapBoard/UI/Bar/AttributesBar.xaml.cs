using FzLib.Extension;
using FzLib.WPF.Dialog;
using MapBoard.Main.UI.Map;
using MapBoard.Main.UI.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
            MapView.Selection.CollectionChanged += (s, e) => ExpandOrCollapse();
        }

        private FeatureAttributeCollection attributes;
        public override FeatureAttributeCollection Attributes => attributes;

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
                    attributes = FeatureAttributeCollection.FromFeature(MapView.Layers.Selected, MapView.Selection.SelectedFeatures.First());
                    dataGrid.Columns[1].IsReadOnly = true;
                    break;

                default:
                    Collapse();
                    return;
            }
            this.Notify(nameof(Attributes));
            Expand();
        }

        private void TextBlock_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!dataGrid.Columns[1].IsReadOnly)
            {
                return;
            }
            string text = (sender as TextBlock).Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (File.Exists(text))
            {
                try
                {
                    IOUtility.OpenFileOrFolder(text);
                    return;
                }
                catch (Exception ex)
                {
                }
            }
            if (Directory.Exists(text))
            {
                try
                {
                    IOUtility.OpenFileOrFolder(text);
                    return;
                }
                catch (Exception ex)
                {
                }
            }
            if (Uri.TryCreate(text, UriKind.Absolute, out Uri uriResult)
    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                try
                {
                    IOUtility.OpenFileOrFolder(text);
                    return;
                }
                catch (Exception ex)
                {
                }
            }

            Clipboard.SetText(text);
            SnakeBar.Show($"已复制{text}到剪贴板");
        }

        private void DataGridCell_Selected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(DataGridCell) && MapView.CurrentTask == BoardTask.Draw)
            {
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);
                //找到DataGridCell中第一个（唯一一个）TextBox
                var txt = FzLib.WPF.Common.GetFirstChildOfType<TextBox>(e.OriginalSource as FrameworkElement);
                if (txt != null)
                {
                    //让TextBox获取焦点和键盘焦点
                    txt.Focus();
                    Keyboard.Focus(txt);
                }
            }
        }

        private async void dataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //让编辑控件失去焦点以更新绑定数据源
                (sender as FrameworkElement).Focus();
                //等待一段时间让数据更新
                await Task.Delay(100);
                MapView.Editor.StopAndSave();
            }
        }
    }
}