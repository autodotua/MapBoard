﻿using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common;
using MapBoard.Common.Dialog;
using MapBoard.Common.Resource;
using MapBoard.Main.IO;
using MapBoard.Main.Model;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        }

        private double lineWidth = 5;

        public double LineWidth
        {
            get => lineWidth;
            set => SetValueAndNotify(ref lineWidth, value, nameof(LineWidth));
        }


        public ObservableCollection<KeySymbolPair> Keys { get; set; } = new ObservableCollection<KeySymbolPair>();

        public KeySymbolPair SelectedKey
        {
            get => selectedKey;
            set
            {
                //保存旧值
                if (selectedKey != null)
                {
                    selectedKey.Symbol.LineWidth = LineWidth;
                    selectedKey.Symbol.LineColor = MediaColorToDrawingColor(lineColorPicker.ColorBrush.Color);
                    selectedKey.Symbol.FillColor = MediaColorToDrawingColor(fillColorPicker.ColorBrush.Color);
                }

                SetValueAndNotify(ref selectedKey, value, nameof(SelectedKey));

                btnChangeKey.IsEnabled = btnDeleteKey.IsEnabled = value != null && value.Key != defaultKeyName;
                if (value != null)
                {
                    //应用新值
                    LineWidth = value.Symbol.LineWidth;
                    lineColorPicker.ColorBrush = new SolidColorBrush(DrawingColorToMeidaColor(value.Symbol.LineColor));
                    fillColorPicker.ColorBrush = new SolidColorBrush(DrawingColorToMeidaColor(value.Symbol.FillColor));
                }
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

            if (SelectedKey != null)
            {
                SelectedKey.Symbol.LineWidth = LineWidth;
                SelectedKey.Symbol.LineColor = MediaColorToDrawingColor(lineColorPicker.ColorBrush.Color);
                SelectedKey.Symbol.FillColor = MediaColorToDrawingColor(fillColorPicker.ColorBrush.Color);
            }
            layer.Symbols.Clear();
            foreach (var keySymbol in Keys)
            {
                layer.Symbols.Add(keySymbol.Key == defaultKeyName ? "" : keySymbol.Key, keySymbol.Symbol);
            }
            Label.LineColor = MediaColorToDrawingColor(labelLineColor.ColorBrush.Color);
            Label.FillColor = MediaColorToDrawingColor(labelFillColor.ColorBrush.Color);

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
                    Util.LayerUtility.RemoveLayer(LayerCollection.Instance.Selected, false);
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
                await layer.ApplyLayers();
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

            labelLineColor.ColorBrush = new SolidColorBrush(DrawingColorToMeidaColor(Label.LineColor));
            labelFillColor.ColorBrush = new SolidColorBrush(DrawingColorToMeidaColor(Label.FillColor));
        }

        private void SetScaleButtonClick(object sender, RoutedEventArgs e)
        {
            Label.MinScale = ArcMapView.Instance.MapScale;
        }

        private JObject labelJson;
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
            var keys = (await style.GetAllFeatures()).Select(p => p.GetAttributeValue(Resource.ClassFieldName) as string).Distinct();
            foreach (var key in keys)
            {
                if (Keys.Any(p => p.Key == key) || key == "")
                {
                    continue;
                }
                SymbolInfo symbol = new SymbolInfo()
                {
                    LineColor = GetRandomColor(),
                    FillColor = GetRandomColor(),
                };
                Keys.Add(new KeySymbolPair(key, symbol));
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
}