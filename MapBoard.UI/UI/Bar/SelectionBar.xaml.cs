using Esri.ArcGISRuntime.Geometry;
using FzLib;
using FzLib.WPF.Dialog;
using MapBoard.UI.Dialog;
using MapBoard.Mapping;
using MapBoard.Util;
using ModernWpf.Controls;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MapBoard.Mapping.Model;
using FzLib.WPF;
using MapBoard.IO;
using System.IO;
using MapBoard.UI.Menu;

namespace MapBoard.UI.Bar
{

    /// <summary>
    /// EditBar.xaml 的交互逻辑
    /// </summary>
    public partial class SelectionBar : BarBase
    {
        private FeatureAttributeCollection attributes;

        private SelectFeatureDialog selectFeatureDialog;
        private ShowImageDialog imageDialog;

        public SelectionBar()
        {
            InitializeComponent();
        }

        public override FeatureAttributeCollection Attributes => attributes;

        public bool IsLayerEditable =>
            (Layers?.Selected) != null
            && Layers?.Selected?.CanEdit == true;

        public string Message { get; set; } = "正在编辑";

        protected override ExpandDirection ExpandDirection => ExpandDirection.Down;

        public override void Initialize()
        {
            MapView.BoardTaskChanged += BoardTaskChanged;
            this.GetWindow().SizeChanged += (p1, p2) => ResetDialogLocation();
            this.GetWindow().LocationChanged += (p1, p2) => ResetDialogLocation();
            MapView.Selection.CollectionChanged += SelectedFeaturesChanged;
        }

        private void ResetDialogLocation()
        {
            selectFeatureDialog?.ResetLocation();
            imageDialog?.ResetLocation();
        }

        private void BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask == BoardTask.Select)
            {
                //SelectedFeaturesChanged(null, null);
                Expand();
            }
            else
            {
                Collapse();
            }
        }

        private void BtnExportClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected;
            var features = MapView.Selection.SelectedFeatures.ToArray();

            FeatureLayerMenuHelper menu = new FeatureLayerMenuHelper(this.GetWindow() as MainWindow, MapView, layer, features);
            OpenMenus(menu.GetExportMenus(header => $"正在{header}"), sender as UIElement);

        }


        private void BtnMenuClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected as IEditableLayerInfo;
            if (layer == null)
            {
                return;
            }
            var features = MapView.Selection.SelectedFeatures.ToArray();
            FeatureLayerMenuHelper menu = new FeatureLayerMenuHelper(this.GetWindow() as MainWindow, MapView, layer, features);
            OpenMenus(menu.GetEditMenus(header => $"正在进行{header}操作"), sender as UIElement);

        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            MapView.Selection.ClearSelection();
        }

        private async void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            await (this.GetWindow() as MainWindow).DoAsync(async () =>
            {
                SelectLayerDialog dialog = new SelectLayerDialog(MapView.Layers,
                    p => p.CanEdit && p.GeometryType == MapView.Layers.Selected.GeometryType,
                    true);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    bool copy = Layers.Selected is IEditableLayerInfo ?
                    await CommonDialog.ShowYesNoDialogAsync("是否保留原图层中选中的图形？") : true;

                    await FeatureUtility.CopyOrMoveAsync(Layers.Selected, dialog.SelectedLayer as IEditableLayerInfo, MapView.Selection.SelectedFeatures.ToArray(), copy);
                    MapView.Selection.ClearSelection();
                    Layers.Selected = dialog.SelectedLayer;
                }
            }, "正在复制图形");
        }

        private async void CutButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(Layers.Selected is IEditableLayerInfo);
            var features = MapView.Selection.SelectedFeatures.ToArray();
            var line = await MapView.Editor.GetPolylineAsync();

            if (line != null)
            {
                await (this.GetWindow() as MainWindow).DoAsync(async () =>
                {
                    var result = await FeatureUtility.CutAsync(Layers.Selected as IEditableLayerInfo, features, line);
                    SnakeBar.Show($"已分割为{result.Count}个图形");
                    MapView.Selection.ClearSelection();
                }, "正在分割", true);
            }
        }

        private async void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(Layers.Selected is IEditableLayerInfo);
            await (this.GetWindow() as MainWindow).DoAsync(async () =>
            {
                await FeatureUtility.DeleteAsync(Layers.Selected as IEditableLayerInfo, MapView.Selection.SelectedFeatures.ToArray());
                MapView.Selection.ClearSelection();
            }, "正在删除", true);
        }

        private async void EditButtonClick(object sender, RoutedEventArgs e)
        {
            Debug.Assert(MapView.Selection.SelectedFeatures.Count == 1);
            Debug.Assert(Layers.Selected is IEditableLayerInfo);
            var feature = MapView.Selection.SelectedFeatures.First();
            MapView.Selection.ClearSelection();
            await MapView.Editor.EditAsync(Layers.Selected as IEditableLayerInfo, feature);
        }

        private void Layers_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MapLayerCollection.Selected))
            {
                this.Notify(nameof(IsLayerEditable));
            }
        }

        private void OpenMenus(IEnumerable<MenuItem> menus, UIElement parent)
        {
            ContextMenu menu = new ContextMenu();

            menus.ForEach(p => menu.Items.Add(p));
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = parent;
            menu.IsOpen = true;
        }

        private async void SelectedFeaturesChanged(object sender, EventArgs e)
        {
            if (MapView.CurrentTask != BoardTask.Select)
            {
                OnSelectingImagePointAsync();
                return;
            }
            int count = MapView.Selection.SelectedFeatures.Count;
            btnEdit.IsEnabled = count == 1 && IsLayerEditable;
            //btnMoreAttributes.IsEnabled = count == 1;
            var layer = Layers.Selected;
            btnCut.IsEnabled = (layer.GeometryType == GeometryType.Polygon
                || layer.GeometryType == GeometryType.Polyline)
                && IsLayerEditable;
            btnDelete.IsEnabled = IsLayerEditable;
            btnMenu.IsEnabled = IsLayerEditable;
            attributes = count != 1 ?
                null : FeatureAttributeCollection.FromFeature(layer, MapView.Selection.SelectedFeatures.First());
            LoadSelectFeatureDialog(count);
            await LoadMessages(count, layer);
            Debug.WriteLine(count);
            await OnSelectingImagePointAsync();
        }

        private void LoadSelectFeatureDialog(int count)
        {
            if (count > 1 && (selectFeatureDialog == null || selectFeatureDialog.IsClosed))
            {
                selectFeatureDialog = new SelectFeatureDialog(this.GetWindow(), MapView, MapView.Layers);
                selectFeatureDialog.Show(); ;
            }
        }

        private async Task LoadMessages(int count, IMapLayerInfo layer)
        {
            StringBuilder sb = new StringBuilder($"已选择{MapView.Selection.SelectedFeatures.Count}个图形");
            Message = sb.ToString();
            await Task.Run(() =>
            {
                try
                {
                    switch (layer.GeometryType)
                    {
                        case GeometryType.Point when count == 1:
                            var point = MapView.Selection.SelectedFeatures.First().Geometry as MapPoint;
                            sb.Append($"，经度={point.X:0.0000000}，纬度={point.Y:0.0000000}");
                            break;

                        case GeometryType.Envelope:
                            return;

                        case GeometryType.Polyline:
                            double length = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetLength());
                            sb.Append("，长度：" + NumberConverter.MeterToFitString(length));

                            break;

                        case GeometryType.Polygon:
                            double length2 = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetLength());
                            double area = MapView.Selection.SelectedFeatures.Sum(p => p.Geometry.GetArea());
                            sb.Append("，周长：" + NumberConverter.MeterToFitString(length2));
                            sb.Append("，面积：" + NumberConverter.SquareMeterToFitString(area));

                            break;

                        case GeometryType.Multipoint:
                            int pointCount = MapView.Selection.SelectedFeatures
                            .Select(p => (p.Geometry as Multipoint).Points.Count).Sum();
                            sb.Append($"，{pointCount}个点");
                            break;

                        default:
                            return;
                    }
                }
                catch (InvalidOperationException)
                {
                    //当MapView.Selection.SelectedFeatures集合发生改变时会抛出错误，此时不需要继续进行计算
                    return;
                }
            });
            Message = sb.ToString();

            this.Notify(nameof(Attributes));
        }

        private async Task OnSelectingImagePointAsync()
        {
            if (MapView.Selection.SelectedFeatures.Count != 1)
            {
                if (imageDialog != null && !imageDialog.IsClosed)
                {
                    imageDialog.Close();
                }
                return;
            }
            var feature = MapView.Selection.SelectedFeatures.FirstOrDefault();
            if (feature.Attributes.ContainsKey(Photo.ImagePathField))
            {
                string path = feature.GetAttributeValue(Photo.ImagePathField) as string;
                if (path != null && File.Exists(path))
                {
                    if (imageDialog == null || imageDialog.IsClosed)
                    {
                        imageDialog = new ShowImageDialog(this.GetWindow());
                        imageDialog.Show();
                    }
                    await imageDialog.SetImageAsync(path);
                }
            }
        }

        private void ValueTextBlock_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tbk)
            {
                if (!string.IsNullOrEmpty(tbk.Text))
                {
                    Clipboard.SetText(tbk.Text);
                    SnakeBar.Show($"已复制“{tbk.Text}”到剪贴板");
                }
            }
        }
    }
}