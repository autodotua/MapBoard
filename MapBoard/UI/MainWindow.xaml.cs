using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using FzLib.Geography.Coordinate;
using FzLib.Geography.Coordinate.Convert;
using FzLib.Program;
using MapBoard.Format;
using MapBoard.Style;
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
using System.Runtime.InteropServices;
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

        private static readonly TwoWayDictionary<string, SketchCreationMode> ButtonsMode = new TwoWayDictionary<string, SketchCreationMode>()
        {
            { "多段线",SketchCreationMode.Polyline},
            { "自由线", SketchCreationMode.FreehandLine},
            { "多边形",SketchCreationMode.Polygon},
            { "自由面",SketchCreationMode.FreehandPolygon},
            { "圆",SketchCreationMode.Circle},
            { "椭圆",SketchCreationMode.Ellipse},
            { "箭头",SketchCreationMode.Arrow},
            { "矩形", SketchCreationMode.Rectangle},
            { "三角形",SketchCreationMode.Triangle},
            { "点",SketchCreationMode.Point},
            { "多点",SketchCreationMode.Multipoint},
        };

        public string[] LineTypes { get; } = { "多段线", "自由线" };

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            SnakeBar.DefaultWindow = this;
            RegistEvents();

            ResetStyleSettingUI();
        }

        private void RegistEvents()
        {
            arcMap.ViewpointChanged += ArcMapViewpointChanged;
            arcMap.Selection.SelectedFeatures.CollectionChanged += (p1, p2) =>
            {
                JudgeControlsEnable();
                //btnSelect.IsChecked = arcMap.Selection.SelectedFeatures.Count > 0;
            };
            BoardTaskManager.BoardTaskChanged += (s, e) =>
              {
                  JudgeControlsEnable();

                  //if (e.NewTask == BoardTaskManager.BoardTask.Draw)
                  //{
                  //    grdButtons.Children.Cast<ToggleButton>().First(b => b.Content.Equals(ButtonsMode.GetKey(arcMap.Drawing.LastDrawMode.Value))).IsChecked = true;
                  //}

                  //else if (e.OldTask == BoardTaskManager.BoardTask.Draw && e.NewTask == BoardTaskManager.BoardTask.Ready)
                  //{
                  //    grdButtons.Children.Cast<ToggleButton>().First(p => p.IsChecked == true).IsChecked = false;
                  //}
              };
            //arcMap.Editing.EditingStatusChanged += (p1, p2) => JudgeControlsEnable();

            //arcMap.Drawing.DrawStatusChanged += (p1, p2) =>
            //{

            //};


            var lvwHelper = new FzLib.Control.Extension.ListViewHelper<StyleInfo>(lvw);
            lvwHelper.EnableDragAndDropItem();
            //lvwHelper.SingleItemDragDroped += (s, e) => arcMap.Map.OperationalLayers.Move(e.OldIndex, e.NewIndex);
        }

        private void JudgeControlsEnable()
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
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
            btnApplyStyle.IsEnabled = btnBrowseMode.IsEnabled = grdStyleSetting.IsEnabled = StyleCollection.Instance.Selected != null;



            //if (arcMap.Selection.IsSelecting)
            //{
            //    await arcMap.Selection.StopSelect(false);
            //}
            if (IsLoaded)
            {
                btnSelect.IsEnabled = StyleCollection.Instance.Selected != null;
                grdButtons.IsEnabled = StyleCollection.Instance.Selected == null || StyleCollection.Instance.Selected.LayerVisible;

                var buttons = grdButtons.Children.Cast<FrameworkElement>().Where(p => p is SplitButton.SplitButton);//.Cast<ToggleButton>();
                buttons.ForEach(p => p.Visibility = Visibility.Collapsed);
                if (StyleCollection.Instance.Selected != null)
                {
                    switch (StyleCollection.Instance.Selected.Type)
                    {
                        case GeometryType.Multipoint:
                            splBtnMultiPoint.Visibility = Visibility.Visible;
                            break;
                        case GeometryType.Point:
                            splBtnPoint.Visibility = Visibility.Visible;
                            break;
                        case GeometryType.Polyline:
                            splBtnPolyline.Visibility = Visibility.Visible;
                            break;
                        case GeometryType.Polygon:
                            splBtnPolygon.Visibility = Visibility.Visible;
                            break;
                    }
                }
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




        private async void UrlTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await arcMap.LoadBasemap();
            }
        }
        private async void DrawButtonsClick(object sender, RoutedEventArgs e)
        {
            string text = null;
            if (sender is SplitButton.SplitButton)
            {
                text = (sender as SplitButton.SplitButton).Text;
            }
            else
            {
                text = (sender as MenuItem).Header as string;
            }
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
            {
                await arcMap.Selection.StopFrameSelect(false);
                //btnSelect.IsChecked = false;
            }

            var mode = ButtonsMode[text];
            await arcMap.Drawing.StartDraw(mode);
        }


        private async void SelectToggleButtonClick(object sender, RoutedEventArgs e)
        {
            //if (StyleCollection.Instance.Selected == null)
            //{
            //    SnakeBar.ShowError("没有选择任何样式");
            //    btnSelect.IsChecked = false;
            //    return;
            //}
            //if (btnSelect.IsChecked == true)
            //{
            //if (lastBtn != null)
            //{
            //    lastBtn.IsChecked = false;
            //    await arcMap.Drawing.StopDraw();
            //    lastBtn = null;
            //}
            await arcMap.Selection.StartSelect(SketchCreationMode.Rectangle);
            //}
            //else
            //{
            //    await arcMap.Selection.StopFrameSelect(false);
            //}

        }


        private void ImportBtnClick(object sender, RoutedEventArgs e)
        {
            string path = CommonFileSystemDialog.GetOpenFile(new List<(string, string)>()
            { ("mbpkg地图画板包", "mbpkg"),("GPS轨迹文件","gpx") }, false, true);
            if (path != null)
            {
                loading.Show();
                switch (Path.GetExtension(path))
                {
                    case ".mbpkg":
                        try
                        {
                            Mbpkg.Import(path);
                            restart = true;
                        }
                        catch (Exception ex)
                        {
                            restart = false;
                            TaskDialog.ShowException(this, ex, "导入失败");
                        }
                        break;

                    case ".gpx":
                        try
                        {
                            TaskDialog.ShowWithCommandLinks(this, "请选择转换类型", "正在准备导入GPS轨迹文件",
                           new (string, string, Action)[] {
                                ("点","每一个轨迹点分别加入到新的样式中",()=>Gpx.Import(path,Gpx.Type.Point)),
                                ("一条线","按时间顺序将轨迹点相连，形成一条线",()=>Gpx.Import(path,Gpx.Type.OneLine)),
                                ("多条线","按时间顺序将每两个轨迹点相连，形成n-1条线",()=>Gpx.Import(path,Gpx.Type.MultiLine)),
                           });
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.ShowException(this, ex, "导入失败");
                        }
                        break;
                }
            }
            loading.Hide();
        }

        private void ExportBtnClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Config.DataPath))
            {
                SnakeBar.ShowError("数据目录" + Config.DataPath + "不存在");
                return;
            }
            string path = CommonFileSystemDialog.GetSaveFile(new List<(string, string)>() { ("mbpkg地图画板包", "mbpkg") }, false, true, "地图画板 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            if (path != null)
            {
                try
                {
                    Mbpkg.Export(path);
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

        private void ListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteStyle(null, null);
            }
        }

        private async void CopyFeatures(object sender, RoutedEventArgs e)
        {

            SelectStyleDialog dialog = new SelectStyleDialog(this);
            if (dialog.ShowDialog() == true)
            {
                FeatureQueryResult features = await StyleCollection.Instance.Selected.GetAllFeatures();
                ShapefileFeatureTable targetTable = dialog.SelectedStyle.Table;

                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                dialog.SelectedStyle.UpdateFeatureCount();
            }
        }

        private async void SaveImage(object sender, RoutedEventArgs e)
        {
            RuntimeImage image = await arcMap.ExportImageAsync();
            var bitmap = ConvertToBitmap(await image.ToImageSourceAsync() as BitmapSource);
            // string path=CommonFileSystemDialog.GetSaveFile()
        }


        public static Bitmap ConvertToBitmap(BitmapSource bitmapSource)
        {
            var width = bitmapSource.PixelWidth;
            var height = bitmapSource.PixelHeight;
            var stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
            bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
            var bitmap = new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, memoryBlockPointer);
            return bitmap;
        }
        private void DeleteStyle(object sender, RoutedEventArgs e)
        {
            if (StyleCollection.Instance.Selected == null)
            {
                SnakeBar.ShowError("没有选择任何样式");
                return;
            }
            var style = StyleCollection.Instance.Selected;
            StyleHelper.RemoveStyle(style, true);
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


        private void CreateStyleButtonClick(object sender, RoutedEventArgs e)
        {
            TaskDialog.ShowWithCommandLinks(null, "请选择类型", new (string, string, Action)[]
            {
                ("线",null,()=>StyleHelper.CreateStyle(GeometryType.Polyline)),
                ("面",null,()=>StyleHelper.CreateStyle(GeometryType.Polygon)),
                ("点",null,()=>StyleHelper.CreateStyle(GeometryType.Point)),
                ("多点",null,()=>StyleHelper.CreateStyle(GeometryType.Multipoint)),
            }, null, Microsoft.WindowsAPICodePack.Dialogs.TaskDialogStandardIcon.None, true);
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
                    StyleHelper.RemoveStyle(StyleCollection.Instance.Selected, false);
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
                style.Table = null;
                StyleCollection.Instance.Styles.Add(style);
            }
            else
            {
                arcMap.SetLayerProperties(style);
            }
            StyleCollection.Instance.Save();
        }

        private void SelectedStyleChanged(object sender, SelectionChangedEventArgs e)
        {


            JudgeControlsEnable();
            ResetStyleSettingUI();
        }

        private void ResetStyleSettingUI()
        {
            if (!IsLoaded || StyleCollection.Instance.Selected == null)
            {
                return;
            }
            else
            {
                txtName.Text = StyleCollection.Instance.Selected?.Name;

                txtLineWidth.Text = StyleCollection.Instance.Selected.LineWidth.ToString();
                lineColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(Styles.Selected.LineColor));
                fillColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(Styles.Selected.FillColor));

            }
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //btnSelect.IsEnabled = StyleCollection.Instance.Selected != null;
            //grdButtons.IsEnabled = StyleCollection.Instance.Selected == null || StyleCollection.Instance.Selected.LayerVisible;
            JudgeControlsEnable();

            if (StyleCollection.Instance.Selected != null && StyleCollection.Instance.Selected.FeatureCount > 0)
            {
                await arcMap.SetViewpointGeometryAsync(await StyleCollection.Instance.Selected.Table.QueryExtentAsync(new QueryParameters()));
            }
        }


        private void ListItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var style = StyleCollection.Instance.Selected;
            ContextMenu menu = new ContextMenu();

            List<(string header, RoutedEventHandler action, bool visiable)> menus = new List<(string header, RoutedEventHandler action, bool visiable)>()
           {
                ("复制",CopyFeatures,true),
                ("转面",PolylineToPolygon,style.Type==GeometryType.Polyline),
                ("删除",DeleteStyle,true),
                ("新建副本",CreateCopy,true),
                ("缩放到图层", async (p1, p2) => await arcMap.SetViewpointGeometryAsync(await style.Table.QueryExtentAsync(new QueryParameters())),StyleCollection.Instance.Selected.FeatureCount > 0)

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

        private void CreateCopy(object sender, RoutedEventArgs e)
        {
            TaskDialog.ShowWithCommandLinks("是否要复制所有图形到新的样式中", "请选择副本类型", new (string, string, Action)[]
            {
                ("仅样式",null,()=> CopyStyle()),
                ("样式和所有图形",null,CopyAll)
            });

            async void CopyAll()
            {
                FeatureQueryResult features = await StyleCollection.Instance.Selected.GetAllFeatures();

                var style = CopyStyle();
                ShapefileFeatureTable targetTable = style.Table;

                foreach (var feature in features)
                {
                    await targetTable.AddFeatureAsync(feature);
                }
                style.UpdateFeatureCount();
            }

            StyleInfo CopyStyle()
            {

                StyleInfo style = StyleCollection.Instance.Selected;
                return StyleHelper.CreateStyle(style.Type, style);
            }
        }

        private void btnBrowseMode_Click(object sender, RoutedEventArgs e)
        {
            StyleCollection.Instance.Selected = null;
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
