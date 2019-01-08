﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Control.Dialog;
using FzLib.Geography.Coordinate;
using FzLib.Geography.Coordinate.Convert;
using FzLib.Program;
using MapBoard.Code;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static FzLib.Basic.Collection.Loop;
using static FzLib.Geography.Analysis.SpeedAnalysis;

namespace MapBoard.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public Config Config => Config.Instance;
        public StyleCollection Styles => StyleCollection.Instance;
        /// <summary>
        /// 控制在执行耗时工作时控件的可用性
        /// </summary>
        private bool controlsEnable = true;
        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            SnakeBar.DefaultWindow = this;
            RegistEvents();

            ResetStyleSettingUI();
            btnSelect.IsEnabled = StyleCollection.Instance.Selected != null;
            grdButtons.IsEnabled = StyleCollection.Instance.Selected.LayerVisible;
        }

        private void RegistEvents()
        {
            arcMap.ViewpointChanged += ArcMapViewpointChanged;
            arcMap.Selection.SelectedFeatures.CollectionChanged += (p1, p2) =>
            {
                JudgeControlsEnable();
                btnSelect.IsChecked = arcMap.Selection.SelectedFeatures.Count > 0;
            };
            BoardTaskManager.BoardTaskChanged += (s, e) =>
              {
                  if (e.IsTaskChanged(BoardTaskManager.BoardTask.Edit))
                  {
                      JudgeControlsEnable();
                  }

                  if (e.IsTaskChanged(BoardTaskManager.BoardTask.Draw))
                  {
                      ToggleButton btn = grdButtons.Children.Cast<ToggleButton>()
                      .First(b => b.Content as string == ButtonsMode.First(p =>
                      p.Mode == arcMap.Drawing.LastDrawMode).Name);
                      btn.IsChecked = e.NewTask == BoardTaskManager.BoardTask.Draw;
                  }
              };
            //arcMap.Editing.EditingStatusChanged += (p1, p2) => JudgeControlsEnable();

            //arcMap.Drawing.DrawStatusChanged += (p1, p2) =>
            //{

            //};


            var lvwHelper = new FzLib.Control.Extension.ListViewHelper<StyleInfo>(lvw);
            lvwHelper.EnableDragAndDropItem();
            lvwHelper.SingleItemDragDroped += (s, e) => arcMap.Map.OperationalLayers.Move(e.OldIndex, e.NewIndex);
        }

        private void JudgeControlsEnable()
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw)
            {
                grdLeft.IsEnabled = false;
            }
            else
            {
                if (arcMap.Selection.SelectedFeatures.Count > 0)
                {
                    lvw.IsEnabled = false;
                }
                else
                {
                    lvw.IsEnabled = true;
                }
                grdLeft.IsEnabled = true;
            }
        }
        //private StyleInfo editingStyle;

        //public StyleInfo EditingStyle
        //{
        //    get => editingStyle;
        //    set
        //    {
        //        isChangingStyle = true;
        //        editingStyle = value;
        //        btnApplyStyle.IsEnabled = btnDefaultStyle.IsEnabled =txtName.IsEnabled= StyleCollection.Instance.Selected!=null;

        //        txtName.Text = StyleCollection.Instance.Selected?.Name;


        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditingStyle)));
        //        lineColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value.LineColor));
        //        fillColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value.FillColor));
        //        isChangingStyle = false;
        //    }
        //}

        /// <summary>
        /// 地图的显示区域发生改变，清空选择框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArcMapViewpointChanged(object sender, EventArgs e)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;



        bool restart = false;
        private async void WindowClosing(object sender, CancelEventArgs e)
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit)
            {
                await arcMap.Editing.StopEditing();
            }
            if (!restart)
            {
                Config.Save();
            }

        }
        public bool ControlsEnable
        {
            get => controlsEnable;
            set
            {
                controlsEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ControlsEnable)));
            }
        }



        private async void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await arcMap.LoadBasemap();
            }
        }
        ToggleButton lastBtn = null;
        private async void DrawButtonsClick(object sender, RoutedEventArgs e)
        {
            ToggleButton btn = sender as ToggleButton;
            if (StyleCollection.Instance.Selected == null)
            {
                SetStyleFromUI(Config.DefaultStyle);
            }
            if (btn.IsChecked == false)
            {
                await arcMap.Drawing.StopDraw();
                lastBtn = null;
            }
            else
            {
                if(BoardTaskManager.CurrentTask==BoardTaskManager.BoardTask.Select)
                {
                    await arcMap.Selection.StopFrameSelect(false);
                    //btnSelect.IsChecked = false;
                }
                if (lastBtn != null)
                {
                    lastBtn.IsChecked = false;

                    await arcMap.Drawing.StopDraw();
                }


                //btn.IsChecked = true;
                lastBtn = btn;
                var mode = ButtonsMode.First(p => p.Name.Equals(btn.Content)).Mode;
                await arcMap.Drawing.StartDraw(mode);
            }
        }

        private (string Name, SketchCreationMode Mode)[] ButtonsMode = new (string, SketchCreationMode)[]
        {
                    ( "直线段",SketchCreationMode.Polyline),
                    ( "自由线", SketchCreationMode.FreehandLine),
                    ( "多边形",SketchCreationMode.Polygon),
                    ( "自由面",SketchCreationMode.FreehandPolygon),
                    ( "圆",SketchCreationMode.Circle),
                    ( "椭圆",SketchCreationMode.Ellipse),
                    ( "箭头",SketchCreationMode.Arrow),
                    ( "矩形", SketchCreationMode.Rectangle),
                    ( "三角形",SketchCreationMode.Triangle),
                    ( "点",SketchCreationMode.Point),
                    ( "多点",SketchCreationMode.Multipoint),
        };

        private async void SelectToggleButtonClick(object sender, RoutedEventArgs e)
        {
            if (StyleCollection.Instance.Selected == null)
            {
                SnakeBar.ShowError("没有选择任何样式");
                btnSelect.IsChecked = false;
                return;
            }
            if (btnSelect.IsChecked == true)
            {
                if (lastBtn != null)
                {
                    lastBtn.IsChecked = false;
                    await arcMap.Drawing.StopDraw();
                    lastBtn = null;
                }
                await arcMap.Selection.StartSelect(SketchCreationMode.Rectangle);
            }
            else
            {
                await arcMap.Selection.StopFrameSelect(false);
            }

        }


        private void ImportBtnClick(object sender, RoutedEventArgs e)
        {
            string path = CommonFileSystemDialog.GetOpenFile(new List<(string, string)>() { ("ZIP压缩文件", "zip") }, false, true);
            if (path != null)
            {
                try
                {
                    StyleCollection.Instance.Styles.ForEach(p => p.Table.Close());
                    if (Directory.Exists(Config.DataPath))
                    {
                        Directory.Delete(Config.DataPath, true);
                    }
                    ZipFile.ExtractToDirectory(path, Config.DataPath);

                    restart = true;
                    Information.Restart();
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(this, ex, "导出失败");
                }
            }
        }

        private async void ExportBtnClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Config.DataPath))
            {
                SnakeBar.ShowError("数据目录" + Config.DataPath + "不存在");
                return;
            }
            string path = CommonFileSystemDialog.GetSaveFile(new List<(string, string)>() { ("ZIP压缩文件", "zip") }, false, true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (path != null)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    StyleCollection.Instance.Styles.ForEach(p => p.Table.Close());
                    ZipFile.CreateFromDirectory(Config.DataPath, path);
                    await arcMap.LoadLayers();
                    SnakeBar.Show("导出成功");
                }
                catch (Exception ex)
                {
                    TaskDialog.ShowException(this, ex, "导出失败");
                }
            }
        }

        private async void PolylineToPolygon(object sender, RoutedEventArgs e)
        {
            if (StyleCollection.Instance.Selected == null)
            {
                return;
            }
            if (StyleCollection.Instance.Selected.Table.GeometryType != GeometryType.Polyline)
            {
                SnakeBar.ShowError("只有线可以执行此命令");
                return;
            }
            await arcMap.PolylineToPolygon(StyleCollection.Instance.Selected);
        }

        private void ListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteStyle(null, null);
            }
        }

        private async void CopyFeatures(object sender, RoutedEventArgs e)
        {
            if (StyleCollection.Instance.Selected == null)
            {
                SnakeBar.ShowError("还没有选择");
                return;
            }
            SelectStyleDialog dialog = new SelectStyleDialog(this);
            if (dialog.ShowDialog() == true)
            {
                StyleCollection.Instance.Selected.LayerVisible = false;
                FeatureQueryResult features = await StyleCollection.Instance.Selected.GetAllFeatures();
                ShapefileFeatureTable targetTable = dialog.SelectedStyle.Table;

                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                dialog.SelectedStyle.UpdateFeatureCount();
            }
        }

        private void DeleteStyle(object sender, RoutedEventArgs e)
        {
            if (StyleCollection.Instance.Selected == null)
            {
                SnakeBar.ShowError("没有选择任何样式");
                return;
            }
            var style = StyleCollection.Instance.Selected;
            arcMap.RemoveStyle(style, true);
        }

        private void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Config.DataPath))
            {
                Process.Start(Config.DataPath);
            }
            else
            {
                SnakeBar.ShowError("目录不存在");
            }
        }


        private void DefaultStyleButtonClick(object sender, RoutedEventArgs e)
        {
            StyleCollection.Instance.Selected = null;
        }

        private void SetStyleFromUI(StyleInfo style)
        {
            style.LineColor = FzLib.Media.Converter.MediaColorToDrawingColor(lineColorPicker.ColorBrush.Color);
            style.FillColor = FzLib.Media.Converter.MediaColorToDrawingColor(fillColorPicker.ColorBrush.Color);

            if (double.TryParse(txtLineWidth.Text, out double result))
            {
                style.LineWidth = result;
            }
            else
            {
                txtLineWidth.Text = style.LineWidth.ToString();
            }
        }

        private async void ApplyStyleButtonClick(object sender, RoutedEventArgs e)
        {
            var style = StyleCollection.Instance.Selected;
            SetStyleFromUI(StyleCollection.Instance.Selected);

            string newName = txtName.Text;
            if (newName != style.Name)
            {
                if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newName.Length > 240)
                {
                    SnakeBar.ShowError("新文件名不合法");
                    goto end;
                }
                if (File.Exists(Path.Combine(Config.DataPath, newName + ".shp")))
                {
                    SnakeBar.ShowError("文件已存在");
                }
                try
                {
                    arcMap.RemoveStyle(StyleCollection.Instance.Selected, false);
                    foreach (var file in Directory.EnumerateFiles(Config.DataPath))
                    {
                        if (Path.GetFileNameWithoutExtension(file) == style.Name)
                        {
                            File.Move(file, Path.Combine(Config.DataPath, newName + Path.GetExtension(file)));
                        }
                    }
                    style.Name = newName;
                }
                catch (Exception ex)
                {
                    SnakeBar.ShowException(ex, "重命名失败");
                }
                end:
                await arcMap.LoadLayer(style);
            }
            else
            {
                arcMap.SetRenderer(style);
            }
            Config.Save();
        }

        private void SelectedStyleChanged(object sender, SelectionChangedEventArgs e)
        {
           btnApplyStyle.IsEnabled = btnDefaultStyle.IsEnabled = txtName.IsEnabled = StyleCollection.Instance.Selected != null;

            //if (arcMap.Selection.IsSelecting)
            //{
            //    await arcMap.Selection.StopSelect(false);
            //}
            if (btnSelect!=null)
            {
                btnSelect.IsEnabled = StyleCollection.Instance.Selected != null ;
                grdButtons.IsEnabled = StyleCollection.Instance.Selected.LayerVisible;
            }


            ResetStyleSettingUI();
        }

        private void ResetStyleSettingUI()
        {
            txtName.Text = StyleCollection.Instance.Selected?.Name;

            txtLineWidth.Text = StyleCollection.Instance.Current.LineWidth.ToString();
            lineColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(Styles.Current.LineColor));
            fillColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(Styles.Current.FillColor));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void listView1_MouseMove(object sender, MouseEventArgs e)
        {
            ListView listview = sender as ListView;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Collections.IList list = listview.SelectedItems as System.Collections.IList;
                DataObject data = new DataObject(typeof(System.Collections.IList), list);
                if (list.Count > 0)
                {
                    DragDrop.DoDragDrop(listview, data, DragDropEffects.Move);
                }
            }
        }

        private void listView1_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(System.Collections.IList)))
            {
                System.Collections.IList peopleList = e.Data.GetData(typeof(System.Collections.IList)) as System.Collections.IList;
                //index为放置时鼠标下元素项的索引  
                int index = GetCurrentIndex(new GetPositionDelegate(e.GetPosition));
                if (index > -1)
                {
                    StyleInfo Logmess = peopleList[0] as StyleInfo;
                    //拖动元素集合的第一个元素索引  
                    int OldFirstIndex = StyleCollection.Instance.Styles.IndexOf(Logmess);
                    //下边那个循环要求数据源必须为ObservableCollection<T>类型，T为对象  
                    for (int i = 0; i < peopleList.Count; i++)
                    {
                        StyleCollection.Instance.Styles.Move(OldFirstIndex, index);
                    }
                    // lvw.SelectedItems.Clear();
                }
            }
        }

        private int GetCurrentIndex(GetPositionDelegate getPosition)
        {
            int index = -1;
            for (int i = 0; i < lvw.Items.Count; ++i)
            {
                ListViewItem item = GetListViewItem(i);
                if (item != null && this.IsMouseOverTarget(item, getPosition))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            System.Windows.Point mousePos = getPosition((IInputElement)target);
            return bounds.Contains(mousePos);
        }

        delegate System.Windows.Point GetPositionDelegate(IInputElement element);

        ListViewItem GetListViewItem(int index)
        {
            if (lvw.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return null;
            return lvw.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
        }

        private void lvw_ItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var style = StyleCollection.Instance.Selected;
            ContextMenu menu = new ContextMenu();

            (string header, RoutedEventHandler action, bool visiable)[] menus = new (string, RoutedEventHandler, bool)[]
           {
                ("复制",CopyFeatures,true),
                ("转线",PolylineToPolygon,style.Type==GeometryType.Polyline),
                ("删除",DeleteStyle,true),
           };

            foreach (var (header, action, visiable) in menus)
            {
                if (visiable)
                {
                    MenuItem item = new MenuItem() { Header = header };
                    item.Click += action;
                    menu.Items.Add(item);
                }
            }

            menu.IsOpen = true;
        }

        //private async void DeleteNoFeatureShpBtnClick(object sender, RoutedEventArgs e)
        //{
        //    var styles = StyleCollection.Instance.Where(p => p.FeatureCount == 0).ToArray();
        //    if(styles.Length==0)
        //    {
        //        SnakeBar.Show("无需优化");
        //    }
        //    else
        //    {
        //        (sender as ButtonBase).IsEnabled = false;
        //        foreach (var style in styles)
        //        {
        //            arcMap.Map.OperationalLayers.Remove(style.Layer);
        //            StyleCollection.Instance.Remove(style);

        //            await Task.Delay(1000);
        //            foreach (var file in Directory.EnumerateFiles(Config.DataPath))
        //            {
        //                if(file.Contains(style.FileName))
        //                {
        //                    File.Delete(file);
        //                }
        //            }
        //        }
        //        (sender as ButtonBase).IsEnabled = true;
        //        SnakeBar.Show("优化完成");
        //    }
        //}

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    switch((sender as FrameworkElement).Tag)
        //    {
        //        case "0":
        //            arcMap.SketchEditor.UndoCommand.Execute(null);
        //            break;
        //        case "1":
        //            arcMap.SketchEditor.RedoCommand.Execute(null);
        //            break;

        //    }
        //}
    }
}
