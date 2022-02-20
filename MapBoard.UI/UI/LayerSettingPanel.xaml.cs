using FzLib;
using FzLib.WPF.Dialog;

using MapBoard.IO;
using MapBoard.Model;
using MapBoard.Mapping;
using MapBoard.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using MapBoard.Mapping.Model;
using MapBoard.UI.Component;
using System.ComponentModel;
using System.Diagnostics;
using PropertyChanged;

namespace MapBoard.UI
{
    /// <summary>
    /// RendererSettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LayerSettingPanel : UserControlBase
    {
        private const string defaultKeyName = "（默认）";

        private Random r = new Random();

        public LayerSettingPanel()
        {
            InitializeComponent();
            Fonts = FontFamily.FamilyNames.Values.ToArray();
        }

        public string[] Fonts { get; }

        public ObservableCollection<KeySymbolPair> Keys { get; set; } = new ObservableCollection<KeySymbolPair>();

        [AlsoNotifyFor(nameof(CanGenerateKey), nameof(CanChangeOrDeleteKey))]
        public FieldInfo KeyField { get; set; } = null;

        public LabelInfo Label { get; set; }

        public ObservableCollection<LabelInfo> Labels { get; set; }

        public string LayerName { get; set; } = "图层名称";

        public MapLayerCollection Layers => MapView?.Layers;

        [AlsoNotifyFor(nameof(Layers))]
        public MainMapView MapView { get; set; }

        public bool CanChangeOrDeleteKey => SelectedKey != null && SelectedKey.Key != defaultKeyName && KeyField != null;
        public bool CanGenerateKey => KeyField != null;

        [AlsoNotifyFor(nameof(CanChangeOrDeleteKey))]
        public KeySymbolPair SelectedKey { get; set; }

        public void Initialize(MainMapView mapView)
        {
            MapView = mapView;
            ResetLayerSettingUI();
            Layers.LayerPropertyChanged += Layers_LayerPropertyChanged;
            Layers.PropertyChanged += Layers_PropertyChanged;
        }

        private void Layers_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Layers.Selected))
            {
                ResetLayerSettingUI();
            }
        }

        private void LoadLabelFieldsMenu()
        {
            menuFields.Items.Clear();
            foreach (var field in Layers.Selected.Fields)
            {
                var item = new MenuItem()
                {
                    Tag = field.Name,
                    Header = $"{field.DisplayName}（{field.Name}）"
                };
                item.Click += (s, e) =>
                {
                    Label.Expression = $"$feature.{(s as MenuItem).Tag as string}";
                };
                menuFields.Items.Add(item);
            }
        }

        public void ResetLayerSettingUI()
        {
            IMapLayerInfo layer = Layers.Selected;
            if (layer == null)
            {
                tab.IsEnabled = false;
                return;
            }
            else
            {
                LayerName = layer.Name;

                Labels = layer.Labels == null ?
                    new ObservableCollection<LabelInfo>() :
                    new ObservableCollection<LabelInfo>(layer.Labels);
                Label = Labels.Count > 0 ? Labels[0] : null;

                Keys.Clear();
                Keys.Add(new KeySymbolPair(defaultKeyName, layer.Renderer.DefaultSymbol ?? layer.GetDefaultSymbol()));
                foreach (var symbol in layer.Renderer.Symbols)
                {
                    Keys.Add(new KeySymbolPair(symbol.Key, symbol.Value));
                }
                SelectedKey = Keys.First(p => p.Key == defaultKeyName);
                KeyField = layer.Fields.FirstOrDefault(p => p.Name == layer.Renderer.KeyFieldName);
                btnClasses.IsEnabled = Layers.Selected is ShapefileMapLayerInfo;
            }
            try
            {
                tab.SelectedIndex = layer.GeometryType switch
                {
                    Esri.ArcGISRuntime.Geometry.GeometryType.Point => 0,
                    Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint => 0,
                    Esri.ArcGISRuntime.Geometry.GeometryType.Polyline => 1,
                    Esri.ArcGISRuntime.Geometry.GeometryType.Polygon => 2,
                    _ => throw new InvalidEnumArgumentException("未知的类型"),
                };
                tab.IsEnabled = true;
            }
            catch (InvalidEnumArgumentException)
            {
                tab.IsEnabled = false;
            }
            LoadLabelFieldsMenu();
        }

        public async Task SetStyleFromUI()
        {
            var layer = Layers.Selected;

            layer.Renderer.Symbols.Clear();
            layer.Renderer.KeyFieldName = KeyField?.Name;
            foreach (var keySymbol in Keys)
            {
                if (keySymbol.Key == defaultKeyName)
                {
                    layer.Renderer.DefaultSymbol = keySymbol.Symbol;
                }
                else
                {
                    layer.Renderer.Symbols.Add(keySymbol.Key, keySymbol.Symbol);
                }
            }
            layer.Labels = Labels.ToArray();
            string newName = LayerName;
            if (newName != layer.Name)
            {
                if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                    || newName.Length > 240 || newName.Length < 1)
                {
                    await CommonDialog.ShowErrorDialogAsync("新文件名不合法");
                }
                else if (File.Exists(Path.Combine(FolderPaths.DataPath, newName + ".shp")))
                {
                    await CommonDialog.ShowErrorDialogAsync("该名称的文件已存在");
                }
                else
                {
                    try
                    {
                        await Layers.Selected.ChangeNameAsync(newName, Layers.EsriLayers);
                    }
                    catch (Exception ex)
                    {
                        App.Log.Error("重命名失败", ex);
                        await CommonDialog.ShowErrorDialogAsync(ex, "重命名失败");
                    }
                    Layers.Selected = layer;
                }
            }
            try
            {
                layer.ApplyStyle();
            }
            catch (Exception ex)
            {
                string error = (string.IsNullOrWhiteSpace(layer.Name) ? "图层" + layer.Name : "图层") + "样式加载失败";
                App.Log.Error(error, ex);
                await CommonDialog.ShowErrorDialogAsync(ex, error);
            }
        }

        private void AddLabelButton_Click(object sender, RoutedEventArgs e)
        {
            Labels.Add(new LabelInfo());
            Label = Labels[^1];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(Label != null);
            Labels.Remove(Label);
            if (Labels.Count > 0)
            {
                Label = Labels[0];
            }
        }

        private async void ChangeKeyButtonClick(object sender, RoutedEventArgs e)
        {
            var key = await CommonDialog.ShowInputDialogAsync("请输入分类名");
            if (key != null)
            {
                if (Keys.Any(p => p.Key == key))
                {
                    await CommonDialog.ShowErrorDialogAsync("该分类已存在");
                    return;
                }

                SelectedKey.Key = key;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (sender as ComboBox).SelectedItem = null;
        }

        private async void CreateKeyButtonClick(object sender, RoutedEventArgs e)
        {
            var key = await CommonDialog.ShowInputDialogAsync("请输入分类名");
            if (key != null)
            {
                if (Keys.Any(p => p.Key == key))
                {
                    await CommonDialog.ShowErrorDialogAsync("该分类已存在");
                    return;
                }

                var keySymbol = new KeySymbolPair(key, MapView.Layers.Selected.GetDefaultSymbol());
                Keys.Add(keySymbol);
                SelectedKey = keySymbol;
            }
        }

        private void DeleteKeyButtonClick(object sender, RoutedEventArgs e)
        {
            Keys.Remove(SelectedKey);
            SelectedKey = Keys[0];
        }

        private async void GenerateKeyButtonClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected;
            Debug.Assert(KeyField != null);
            var keys = (await layer.GetAllFeaturesAsync()).Select(p => p.GetAttributeValue(KeyField.Name) as string).Distinct();
            foreach (var key in keys)
            {
                if (!Keys.Any(p => p.Key == key))
                {
                    SymbolInfo symbol = layer.GetDefaultSymbol();
                    Keys.Add(new KeySymbolPair(key, symbol));
                }
            }
        }

        private void GenerateRandomColorButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var key in Keys)
            {
                key.Symbol.LineColor = GetRandomColor();
                key.Symbol.FillColor = GetRandomColor();
            }
        }

        private Color GetRandomColor()
        {
            int R = r.Next(255);
            int G = r.Next(255);
            int B = r.Next(255);
            B = (R + G > 400) ? R + G - 400 : B;//0 : 380 - R - G;
            B = (B > 255) ? 255 : B;
            return Color.FromArgb(255, R, G, B);
        }

        private void Layers_LayerPropertyChanged(object sender, LayerCollection.LayerPropertyChangedEventArgs e)
        {
            if (e.Layer == MapView.Layers.Selected && e.PropertyName == nameof(MapLayerInfo.IsLoaded))
            {
                ResetLayerSettingUI();
            }
        }

        private void SetScaleButtonClick(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag as string)
            {
                case "label":
                    Label.MinScale = MapView.MapScale;
                    break;

                case "min":
                    Layers.Selected.Display.MinScale = MapView.MapScale;
                    break;

                case "max":
                    Layers.Selected.Display.MaxScale = MapView.MapScale;
                    break;
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            //手动实现手风琴效果
            Expander expander = sender as Expander;
            foreach (var ex in grdExpanders.Children.OfType<Expander>().Where(p => p != sender))
            {
                ex.IsExpanded = false;
            }
            for (int i = 0; i < grdExpanders.Children.Count; i++)
            {
                grdExpanders.RowDefinitions[i].Height = Grid.GetRow(expander) == i ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
            }
        }

        private void ClearKeyFieldButton_Click(object sender, RoutedEventArgs e)
        {
            KeyField = null;
            foreach (var key in Keys.Where(p => p.Key != defaultKeyName).ToList())
            {
                Keys.Remove(key);
            }
        }
    }
}