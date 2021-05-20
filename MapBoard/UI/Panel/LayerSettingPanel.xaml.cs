using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common;

using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
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

namespace MapBoard.Main.UI.Panel
{
    /// <summary>
    /// RendererSettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LayerSettingPanel : ExtendedUserControl
    {
        private const string defaultKeyName = "（默认）";

        public LayerSettingPanel()
        {
            InitializeComponent();
            Fonts = FontFamily.FamilyNames.Values.ToArray();
        }

        public string[] Fonts { get; }

        public ObservableCollection<KeySymbolPair> Keys { get; set; } = new ObservableCollection<KeySymbolPair>();

        public KeySymbolPair SelectedKey
        {
            get => selectedKey;
            set
            {
                SetValueAndNotify(ref selectedKey, value, nameof(SelectedKey));

                btnChangeKey.IsEnabled = btnDeleteKey.IsEnabled = value != null && value.Key != defaultKeyName;
            }
        }

        private string layerName = "图层名称";

        public string LayerName
        {
            get => layerName;
            set => SetValueAndNotify(ref layerName, value, nameof(LayerName));
        }

        private LabelInfo label;

        public LabelInfo Label
        {
            get => label;
            set => SetValueAndNotify(ref label, value, nameof(Label));
        }

        public async Task SetStyleFromUI()
        {
            var layer = LayerCollection.Instance.Selected;

            layer.Symbols.Clear();
            foreach (var keySymbol in Keys)
            {
                layer.Symbols.Add(keySymbol.Key == defaultKeyName ? "" : keySymbol.Key, keySymbol.Symbol);
            }

            string newName = LayerName;
            if (newName != layer.Name)
            {
                int index = LayerCollection.Instance.Layers.IndexOf(LayerCollection.Instance.Selected);

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
                    await LayerUtility.RemoveLayerAsync(LayerCollection.Instance.Selected, false);
                    foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath, layer.Name))
                    {
                        File.Move(file, Path.Combine(Config.DataPath, newName + Path.GetExtension(file)));
                    }
                    layer.Name = newName;
                }
                catch (Exception ex)
                {
                    SnakeBar.ShowException(ex, "重命名失败");
                }
            end:
                layer.Table = null;
                LayerCollection.Instance.Layers.Insert(index, layer);
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
            LayerInfo layer = LayerCollection.Instance.Selected;
            if (!IsLoaded || layer == null)
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
                    Keys.Add(new KeySymbolPair(defaultKeyName, new SymbolInfo()));
                }
                SelectedKey = Keys.First(p => p.Key == defaultKeyName);
            }

            switch (layer.Type)
            {
                case Esri.ArcGISRuntime.Geometry.GeometryType.Point:
                case Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint:
                    tab.SelectedIndex = 0;
                    break;

                case Esri.ArcGISRuntime.Geometry.GeometryType.Polyline:
                    tab.SelectedIndex = 1;
                    break;

                case Esri.ArcGISRuntime.Geometry.GeometryType.Polygon:
                    tab.SelectedIndex = 2;
                    break;

                default:
                    throw new Exception("未知的类型");
            }
        }

        private void SetScaleButtonClick(object sender, RoutedEventArgs e)
        {
            Label.MinScale = ArcMapView.Instance.MapScale;
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
            var key = await CommonDialog.ShowInputDialogAsync("请输入分类名");
            if (key != null)
            {
                if (Keys.Any(p => p.Key == key))
                {
                    await CommonDialog.ShowErrorDialogAsync("该分类已存在");
                    return;
                }

                var keySymbol = new KeySymbolPair(key, new SymbolInfo());
                Keys.Add(keySymbol);
                SelectedKey = keySymbol;
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
            var style = LayerCollection.Instance.Selected;
            var keys = (await style.GetAllFeaturesAsync()).Select(p => p.GetAttributeValue(Resource.ClassFieldName) as string).Distinct();
            foreach (var key in keys)
            {
                if (!Keys.Any(p => p.Key == key) && key != "")
                {
                    SymbolInfo symbol = new SymbolInfo()
                    {
                        LineColor = GetRandomColor(),
                        FillColor = GetRandomColor(),
                    };
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

    public class KeySymbolPair
    {
        public KeySymbolPair()
        {
        }

        public KeySymbolPair(string key, SymbolInfo symbol)
        {
            Key = key;
            Symbol = symbol;
        }

        public string Key { get; set; }
        public SymbolInfo Symbol { get; set; }
    }

    public class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is Color c)
            {
                return new SolidColorBrush(DrawingColorToMeidaColor(c));
            }
            throw new Exception();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is SolidColorBrush b)
            {
                return MediaColorToDrawingColor(b.Color);
            }
            throw new Exception();
        }
    }
}