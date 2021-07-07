using FzLib.Extension;
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
using static FzLib.Media.Converter;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using MapBoard.Mapping.Model;
using MapBoard.UI.Component;

namespace MapBoard.UI
{
    /// <summary>
    /// RendererSettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LayerSettingPanel : UserControlBase
    {
        private const string defaultKeyName = "（默认）";

        public LayerSettingPanel()
        {
            InitializeComponent();
            Fonts = FontFamily.FamilyNames.Values.ToArray();
        }

        public void Initialize(MainMapView mapView)
        {
            MapView = mapView;
            ResetLayerSettingUI();
        }

        public MainMapView MapView { get; private set; }
        public MapLayerCollection Layers => MapView.Layers;

        public string[] Fonts { get; }

        public ObservableCollection<KeySymbolPair> Keys { get; set; } = new ObservableCollection<KeySymbolPair>();

        public KeySymbolPair SelectedKey
        {
            get => selectedKey;
            set
            {
                this.SetValueAndNotify(ref selectedKey, value, nameof(SelectedKey));

                btnChangeKey.IsEnabled = btnDeleteKey.IsEnabled = value != null && value.Key != defaultKeyName;
            }
        }

        private string layerName = "图层名称";

        public string LayerName
        {
            get => layerName;
            set => this.SetValueAndNotify(ref layerName, value, nameof(LayerName));
        }

        private LabelInfo label;

        public LabelInfo Label
        {
            get => label;
            set => this.SetValueAndNotify(ref label, value, nameof(Label));
        }

        public async Task SetStyleFromUI()
        {
            var layer = Layers.Selected;

            layer.Symbols.Clear();
            foreach (var keySymbol in Keys)
            {
                layer.Symbols.Add(keySymbol.Key == defaultKeyName ? "" : keySymbol.Key, keySymbol.Symbol);
            }

            string newName = LayerName;
            if (newName != layer.Name)
            {
                int index = Layers.IndexOf(Layers.Selected);

                if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                    || newName.Length > 240 || newName.Length < 1)
                {
                    await CommonDialog.ShowErrorDialogAsync("新文件名不合法");
                }
                else if (File.Exists(Path.Combine(Parameters.DataPath, newName + ".shp")))
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
                await CommonDialog.ShowErrorDialogAsync(ex, error);
            }
        }

        public void ResetLayerSettingUI()
        {
            MapLayerInfo layer = Layers.Selected;
            if (layer == null)
            {
                return;
            }
            else
            {
                LayerName = layer?.Name;
                Label = layer.Label;
                Keys.Clear();
                foreach (var symbol in layer.Symbols)
                {
                    if (symbol.Key.Length == 0)
                    {
                        Keys.Add(new KeySymbolPair(defaultKeyName, symbol.Value));
                    }
                    else
                    {
                        Keys.Add(new KeySymbolPair(symbol.Key, symbol.Value));
                    }
                }
                if (!Keys.Any(p => p.Key == defaultKeyName))
                {
                    Keys.Add(new KeySymbolPair(defaultKeyName, MapView.Layers.Selected.GetDefaultSymbol()));
                }
                SelectedKey = Keys.First(p => p.Key == defaultKeyName);
            }

            tab.SelectedIndex = layer.GeometryType switch
            {
                Esri.ArcGISRuntime.Geometry.GeometryType.Point => 0,
                Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint => 0,
                Esri.ArcGISRuntime.Geometry.GeometryType.Polyline => 1,
                Esri.ArcGISRuntime.Geometry.GeometryType.Polygon => 2,
                _ => throw new Exception("未知的类型"),
            };
        }

        private void SetScaleButtonClick(object sender, RoutedEventArgs e)
        {
            Label.MinScale = MapView.MapScale;
        }

        private KeySymbolPair selectedKey;

        private void KeyButtonClick(object sender, RoutedEventArgs e)
        {
            if (ppp.IsOpen)
            {
                ppp.IsOpen = false;
            }
            else
            {
                ppp.PlacementTarget = sender as UIElement;
                ppp.IsOpen = true;
            }
        }

        private async void CreateKeyButtonClick(object sender, RoutedEventArgs e)
        {
            ppp.IsOpen = false;
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

                ppp.IsOpen = true;
            }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            ppp.IsOpen = false;
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

        private void DeleteKeyButtonClick(object sender, RoutedEventArgs e)
        {
            Keys.Remove(SelectedKey);
            SelectedKey = Keys[0];
        }

        private async void GenerateKeyButtonClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected;
            var keys = (await layer.GetAllFeaturesAsync()).Select(p => p.GetAttributeValue(Parameters.ClassFieldName) as string).Distinct();
            foreach (var key in keys)
            {
                if (!Keys.Any(p => p.Key == key) && key != "")
                {
                    SymbolInfo symbol = layer.GetDefaultSymbol();
                    Keys.Add(new KeySymbolPair(key, symbol));
                }
            }
        }

        private Random r = new Random();

        private Color GetRandomColor()
        {
            int R = r.Next(255);
            int G = r.Next(255);
            int B = r.Next(255);
            B = (R + G > 400) ? R + G - 400 : B;//0 : 380 - R - G;
            B = (B > 255) ? 255 : B;
            return Color.FromArgb(255, R, G, B);
        }

        private void GenerateRandomColorButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var key in Keys)
            {
                key.Symbol.LineColor = GetRandomColor();
                key.Symbol.FillColor = GetRandomColor();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (sender as ComboBox).SelectedItem = null;
        }
    }
}