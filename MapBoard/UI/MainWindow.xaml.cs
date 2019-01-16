﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using FzLib.Geography.Coordinate;
using FzLib.Geography.Coordinate.Convert;
using FzLib.Program;
using MapBoard.IO;
using MapBoard.Style;
using MapBoard.UI.Dialog;
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
        #region 字段和属性
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

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region 窗体启动与关闭
        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            SnakeBar.DefaultWindow = this;
            RegistEvents();

        }

        private void RegistEvents()
        {
            arcMap.Selection.SelectedFeatures.CollectionChanged += (p1, p2) =>
            {
                JudgeControlsEnable();
            };
            BoardTaskManager.BoardTaskChanged += (s, e) =>
              {
                  JudgeControlsEnable();
              };


            var lvwHelper = new FzLib.Control.Extension.ListViewHelper<StyleInfo>(lvw);
            lvwHelper.EnableDragAndDropItem();
            //lvwHelper.SingleItemDragDroped += (s, e) => arcMap.Map.OperationalLayers.Move(e.OldIndex, e.NewIndex);
        }
        private async void WindowClosing(object sender, CancelEventArgs e)
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit)
            {
                await arcMap.Editing.StopEditing();
            }

            Config.Save();

        }


        #endregion
        private void JudgeControlsEnable()
        {
            if (BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Draw
                || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Edit
                || BoardTaskManager.CurrentTask == BoardTaskManager.BoardTask.Select)
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
            btnApplyStyle.IsEnabled = btnBrowseMode.IsEnabled = styleSetting.IsEnabled = StyleCollection.Instance.Selected != null;



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
            loading.Show();
            if (IOHelper.Import())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Styles)));
            }
            loading.Hide();
        }

        private async void ExportBtnClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Config.DataPath))
            {
                SnakeBar.ShowError("数据目录" + Config.DataPath + "不存在");
                return;
            }
        await    IOHelper.Export();
        }

        private void ListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteStyle();
            }
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

        private void ApplyStyleButtonClick(object sender, RoutedEventArgs e)
        {
            styleSetting.SetStyleFromUI();
            StyleCollection.Instance.Save();
        }

        private void SelectedStyleChanged(object sender, SelectionChangedEventArgs e)
        {


            JudgeControlsEnable();
            styleSetting.ResetStyleSettingUI();
        }

        private void DeleteStyle()
        {
            if (StyleCollection.Instance.Selected == null)
            {
                SnakeBar.ShowError("没有选择任何样式");
                return;
            }
            var style = StyleCollection.Instance.Selected;
            StyleHelper.RemoveStyle(style, true);
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //btnSelect.IsEnabled = StyleCollection.Instance.Selected != null;
            //grdButtons.IsEnabled = StyleCollection.Instance.Selected == null || StyleCollection.Instance.Selected.LayerVisible;
            JudgeControlsEnable();

            if (StyleCollection.Instance.Selected != null && StyleCollection.Instance.Selected.FeatureCount > 0)
            {
                styleSetting.ResetStyleSettingUI();
                await arcMap.SetViewpointGeometryAsync(await StyleCollection.Instance.Selected.Table.QueryExtentAsync(new QueryParameters()));

            }

        }

        private void ListItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var style = StyleCollection.Instance.Selected;
            ContextMenu menu = new ContextMenu();

            List<(string header, Action action, bool visiable)> menus = new List<(string header, Action action, bool visiable)>()
           {
                ("复制",StyleHelper. CopyFeatures,true),
                ("转面",StyleHelper. PolylineToPolygon,style.Type==GeometryType.Polyline),
                ("删除",DeleteStyle,true),
                ("新建副本",StyleHelper. CreateCopy,true),
                ("缩放到图层", async () => await arcMap.SetViewpointGeometryAsync(await style.Table.QueryExtentAsync(new QueryParameters())),StyleCollection.Instance.Selected.FeatureCount > 0),
                ("导出",  ExportSingle,true),

           };



            foreach (var (header, action, visiable) in menus)
            {
                if (visiable)
                {
                    MenuItem item = new MenuItem() { Header = header };
                    item.Click += (p1, p2) => action();
                    menu.Items.Add(item);
                }
            }

            menu.IsOpen = true;

            void ExportSingle()
            {
                string path = CommonFileSystemDialog.GetSaveFile(new List<(string, string)>() { ("mblpkg地图画板图层包", "mblpkg") }, false, true, "地图画板图层 - " + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                if (path != null)
                {
                    try
                    {
                        Mbpkg.ExportLayer(path, StyleCollection.Instance.Selected);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.ShowException(ex, "导出失败");
                    }
                }
            }

        }

        private void BrowseModeButtonClick(object sender, RoutedEventArgs e)
        {
            StyleCollection.Instance.Selected = null;
        }


    }
}
