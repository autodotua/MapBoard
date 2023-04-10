using MapBoard.Model;
using MapBoard.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
using FzLib;
using Esri.ArcGISRuntime.Data;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Geometry;
using static Esri.ArcGISRuntime.Data.SpatialRelationship;
using MapBoard.Mapping.Model;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 查询要素对话框
    /// </summary>
    public partial class QueryFeaturesDialog : DialogWindowBase
    {
        private IMapLayerInfo layer;

        public QueryFeaturesDialog(Window owner, MainMapView mapView, IMapLayerInfo layer) : base(owner)
        {
            MapView = mapView;
            InitializeComponent();
            Layer = layer;
        }

        /// <summary>
        /// 指定的图层。图层更改后需要更新字段菜单
        /// </summary>
        public IMapLayerInfo Layer
        {
            get => layer;
            set
            {
                layer = value;
                if (menuFields == null)
                {
                    return;
                }
                //选择的图层修改后，更新字段菜单
                menuFields.Items.Clear();
                if (value != null)
                {
                    foreach (var field in value.Fields)
                    {
                        var menu = new MenuItem()
                        {
                            Header = field.DisplayName,
                            Tag = field.Name
                        };
                        menu.Click += (s, e) =>
                        {
                            string text = (s as MenuItem).Tag as string;
                            txtWhere.SelectedText = text;
                        };

                        menuFields.Items.Add(menu);
                    }
                }
            }
        }

        /// <summary>
        /// 地图
        /// </summary>
        public MainMapView MapView { get; }

        /// <summary>
        /// 查询参数
        /// </summary>
        public QueryParameters Parameters { get; } = new QueryParameters();

        /// <summary>
        /// 用于将显示在组合框中的字符串转换到空间关系枚举
        /// </summary>
        public Dictionary<string, SpatialRelationship> Str2SpatialRelationships { get; } = new Dictionary<string, SpatialRelationship>()
        {
            ["相交（Intersects）"] = Intersects,
            ["相关（Relate）"] = Relate,
            ["相等（Equals）"] = SpatialRelationship.Equals,
            ["相离（Disjoint）"] = Disjoint,
            ["外部接触（Touches）"] = Touches,
            ["相交且交集维度小于最大纬度（Corsses）"] = Crosses,
            ["被包含（Within）"] = Within,
            ["包含（Contains）"] = Contains,
            ["同纬度相交但不相同（Overlaps）"] = Overlaps,
            ["包围盒相交（EnvelopeIntersects）"] = EnvelopeIntersects,
        };

        /// <summary>
        /// 单击清除图形筛选条件按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            Parameters.Geometry = null;
            this.Notify(nameof(Parameters));
        }

        /// <summary>
        /// 单击选择图形子按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ChooseGeometryButton_Click(ModernWpf.Controls.SplitButton sender, ModernWpf.Controls.SplitButtonClickEventArgs args)
        {
            ChooseGeometryButton_Click(sender, (RoutedEventArgs)null);
        }

        /// <summary>
        /// 单击选择图形按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ChooseGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            //隐藏本窗口并激活主窗口
            Hide();
            GetWindow(MapView).Activate();
            try
            {
                Geometry g = ((sender as FrameworkElement).Tag as string) switch
                {
                    "1" => await MapView.Editor.GetRectangleAsync(),
                    "2" => await MapView.Editor.GetPolygonAsync(),
                    "3" => await MapView.Editor.GetPolylineAsync(),
                    "4" => await MapView.Editor.GetPointAsync(),
                    "5" => await MapView.Editor.GetMultiPointAsync(),
                    _ => throw new NotSupportedException(),
                };
                if (g != null)
                {
                    Parameters.Geometry = g;
                    this.Notify(nameof(Parameters));
                }
            }
            catch (Exception ex)
            {
                App.Log.Error("选取范围失败", ex);
                await CommonDialog.ShowErrorDialogAsync(ex, "选取范围失败");
            }
            finally
            {
                Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 单击查询按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (Layer == null)
            {
                await CommonDialog.ShowErrorDialogAsync("请先选择图层");
                return;
            }
            Debug.Assert(Owner is MainWindow);
            Layer.LayerVisible = true;
            try
            {
                IsEnabled = false;
                await (Owner as MainWindow).DoAsync(async () =>
                 {
                     FeatureQueryResult result = await Layer.QueryFeaturesAsync(Parameters);
                     List<Feature> features = null;
                     await Task.Run(() => features = result.ToList());
                     if (features.Count > 0)
                     {
                         MapView.Selection.Select(features, true);
                     }
                     else
                     {
                         IsEnabled = true;
                         await CommonDialog.ShowErrorDialogAsync("没有找到任何符合条件的结果");
                     }
                 }, "正在查询");
            }
            catch (Exception ex)
            {
                App.Log.Error("查询要素失败", ex);
                IsEnabled = true;
                await CommonDialog.ShowErrorDialogAsync(ex, "查询失败");
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}