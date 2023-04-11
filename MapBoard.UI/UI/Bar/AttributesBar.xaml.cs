using FzLib;
using FzLib.WPF;
using FzLib.WPF.Dialog;
using MapBoard.Mapping;
using MapBoard.Mapping.Model;
using MapBoard.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static MapBoard.Mapping.EditorHelper;

namespace MapBoard.UI.Bar
{
    /// <summary>
    /// 属性面板
    /// </summary>
    public partial class AttributesBar : BarBase
    {
        /// <summary>
        /// 要素属性
        /// </summary>
        private FeatureAttributeCollection attributes;

        public AttributesBar()
        {
            InitializeComponent();
            Width = ExpandDistance;
        }

        /// <summary>
        /// 要素属性
        /// </summary>
        public override FeatureAttributeCollection Attributes => attributes;

        /// <summary>
        /// 展开的宽度
        /// </summary>
        public override double ExpandDistance => 240;

        /// <summary>
        /// 展开方向
        /// </summary>
        protected override ExpandDirection ExpandDirection => ExpandDirection.Left;

        /// <summary>
        /// 初始化
        /// </summary>
        public override void Initialize()
        {
            MapView.BoardTaskChanged += (s, e) => ExpandOrCollapse();
            MapView.Selection.CollectionChanged += (s, e) => ExpandOrCollapse();
        }
        /// <summary>
        /// 按下Ctrl+回车，保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //让编辑控件失去焦点以更新绑定数据源
                (sender as FrameworkElement).Focus();
                var bindings = BindingOperations.GetSourceUpdatingBindingGroups(sender as DataGrid);
                foreach (var binding in bindings)
                {
                    binding.UpdateSources();
                }
                MapView.Editor.StopAndSave();
            }
        }

        /// <summary>
        /// 单元格被选择，直接开始编辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridCell_Selected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(DataGridCell) && MapView.CurrentTask == BoardTask.Draw)
            {
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);
                //找到DataGridCell中第一个（唯一一个）TextBox
                var txt = (e.OriginalSource as FrameworkElement).GetChild<TextBox>();
                if (txt != null)
                {
                    //让TextBox获取焦点和键盘焦点
                    txt.Focus();
                    Keyboard.Focus(txt);
                }
            }
        }

        /// <summary>
        /// 展开或收拢
        /// </summary>
        private void ExpandOrCollapse()
        {
            switch (MapView.CurrentTask)
            {
                case BoardTask.Draw
                            when MapView.Editor.Mode == EditMode.Create
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

        /// <summary>
        /// 鼠标左键按下，如果是文件或目录，那么打开文件或目录。然后放入剪贴板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TextBlock_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
            if (File.Exists(text)
                || Directory.Exists(text)
                || Uri.TryCreate(text, UriKind.Absolute, out Uri uriResult)
    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                await IOUtility.TryOpenInShellAsync(text);
            }
            try
            {
                Clipboard.SetText(text);
                SnakeBar.Show(this.GetWindow(), $"已复制{text}到剪贴板");
            }
            catch(Exception ex)
            {
                SnakeBar.ShowError(this.GetWindow(), ex.Message);
            }
        }
    }
}