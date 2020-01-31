﻿using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common;
using MapBoard.Common.Dialog;
using MapBoard.Common.Resource;
using MapBoard.Main.Helper;
using MapBoard.Main.IO;
using MapBoard.Main.Layer;
using MapBoard.Main.UI.Dialog;
using MapBoard.Main.UI.Map;
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
using Path = System.IO.Path;
using Color = System.Drawing.Color;

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

        public ObservableCollection<KeySymbolPair> Keys { get; set; } = new ObservableCollection<KeySymbolPair>();
        public KeySymbolPair SelectedKey
        {
            get => selectedKey;
            set
            {
                if (selectedKey != null)
                {
                    selectedKey.Symbol.LineWidth = LineWidth;
                    selectedKey.Symbol.LineColor = LineColor;
                    selectedKey.Symbol.FillColor = FillColor;
                }

                SetValueAndNotify(ref selectedKey, value, nameof(SelectedKey));

                btnChangeKey.IsEnabled = btnDeleteKey.IsEnabled = value != null && value.Key != defaultKeyName;
                if (value != null)
                {
                    //LineWidth = LayerCollection.Instance.Selected.Renderer.LineWidth;
                    //LineColor = LayerCollection.Instance.Selected.Renderer.LineColor;
                    //FillColor = LayerCollection.Instance.Selected.Renderer.FillColor;
                    LineWidth = value.Symbol.LineWidth;
                    LineColor = value.Symbol.LineColor;
                    FillColor = value.Symbol.FillColor;
                }
            }
        }

        private string styleName;
        public string StyleName
        {
            get => styleName;
            set => SetValueAndNotify(ref styleName, value, nameof(StyleName));
        }

        private double lineWidth = 5;
        public double LineWidth
        {
            get => lineWidth;
            set => SetValueAndNotify(ref lineWidth, value, nameof(LineWidth));
        }

        public System.Drawing.Color LineColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(lineColorPicker.ColorBrush.Color);
            set => lineColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }


        public System.Drawing.Color FillColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(fillColorPicker.ColorBrush.Color);
            set => fillColorPicker.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }

        public System.Drawing.Color LabelLineColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(labelLineColor.ColorBrush.Color);
            set => labelLineColor.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }


        public System.Drawing.Color LabelFillColor
        {
            get => FzLib.Media.Converter.MediaColorToDrawingColor(labelFillColor.ColorBrush.Color);
            set => labelFillColor.ColorBrush = new SolidColorBrush(FzLib.Media.Converter.DrawingColorToMeidaColor(value));
        }

        private double labelFontSize;
        public double LabelFontSize
        {
            get => labelFontSize;
            set => SetValueAndNotify(ref labelFontSize, value, nameof(LabelFontSize));
        }
        private double labelStrokeThickness;
        public double LabelStrokeThickness
        {
            get => labelStrokeThickness;
            set => SetValueAndNotify(ref labelStrokeThickness, value, nameof(LabelStrokeThickness));
        }

        private double labelMinScale;
        public double LabelMinScale
        {
            get => labelMinScale;
            set
            {
                if (value >= 0)
                {
                    labelMinScale = value;
                }
                else
                {
                    labelMinScale = 0;
                }
                Notify(nameof(LabelMinScale));
            }
        }

        public void SetStyleFromUI()
        {
            var style = LayerCollection.Instance.Selected;
            //style.Renderer.LineColor = LineColor;
            //style.Renderer.FillColor = FillColor;
            //style.Renderer.LineWidth = LineWidth;
            if (SelectedKey != null)
            {
                SelectedKey.Symbol.LineWidth = LineWidth;
                SelectedKey.Symbol.LineColor = LineColor;
                SelectedKey.Symbol.FillColor = FillColor;
            }
            style.Symbols.Clear();
            foreach (var keySymbol in Keys)
            {
                style.Symbols.Add(keySymbol.Key == defaultKeyName ? "" : keySymbol.Key, keySymbol.Symbol);
            }

            SetLabelFromUI();
            //Layersetting.SetStyleFromUI(LayerCollection.Instance.Selected);

            string newName = StyleName;
            if (newName != style.Name)
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
                    Helper.LayerHelper.RemoveLayer(LayerCollection.Instance.Selected, false);
                    foreach (var file in Shapefile.GetExistShapefiles(Config.DataPath, style.Name))
                    {
                        File.Move(file, Path.Combine(Config.DataPath, newName + Path.GetExtension(file)));
                    }
                    style.Name = newName;
                }
                catch (Exception ex)
                {
                    SnakeBar.ShowException(ex, "重命名失败");
                }
            end:
                style.Table = null;
                LayerCollection.Instance.Layers.Insert(index, style);
            }
            else
            {
                style.ApplyLayers();
            }

        }

        public void ResetLayersettingUI()
        {
            if (!IsLoaded || LayerCollection.Instance.Selected == null)
            {
                return;
            }
            else
            {
                StyleName = LayerCollection.Instance.Selected?.Name;

                //LineWidth = LayerCollection.Instance.Selected.Renderer.LineWidth;
                //LineColor = LayerCollection.Instance.Selected.Renderer.LineColor;
                //FillColor = LayerCollection.Instance.Selected.Renderer.FillColor;

                Keys.Clear();
                var style = LayerCollection.Instance.Selected;
                foreach (var symbol in style.Symbols)
                {
                    if (symbol.Key == "")
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


                ResetLabelSettingUI();
            }
        }

        private void SetScaleButtonClick(object sender, RoutedEventArgs e)
        {
            LabelMinScale = ArcMapView.Instance.MapScale;
        }
        private JObject labelJson;
        private KeySymbolPair selectedKey;

        private void SetLabelFromUI()
        {
            labelJson = JObject.Parse(Resource.LabelJson);
            SetLabelJsonValue<byte>("symbol.haloColor", GetRgbaFromColor(LabelLineColor));
            SetLabelJsonValue<byte>("symbol.color", GetRgbaFromColor(LabelFillColor));
            SetLabelJsonValue("symbol.font.size", LabelFontSize);
            SetLabelJsonValue("symbol.haloSize", LabelStrokeThickness);
            SetLabelJsonValue("minScale", LabelMinScale);
            LayerCollection.Instance.Selected.LabelJson = labelJson.ToString();
        }
        private void ResetLabelSettingUI()
        {
            labelJson = JObject.Parse(LayerCollection.Instance.Selected.LabelJson);
            LabelLineColor = GetColorFromArgb(GetLabelJsonValue<byte>("symbol.haloColor", new byte[] { 0, 0, 0, 0 }));
            LabelFillColor = GetColorFromArgb(GetLabelJsonValue<byte>("symbol.color", new byte[] { 0, 0, 0, 0 }));
            LabelFontSize = GetLabelJsonValue("symbol.font.size", 9);
            LabelStrokeThickness = GetLabelJsonValue("symbol.haloSize", 1);
            LabelMinScale = GetLabelJsonValue("minScale", 0);
        }
        private static byte[] GetRgbaFromColor(System.Drawing.Color color)
        {
            return new byte[] { color.R, color.G, color.B, color.A };
        }
        private static System.Drawing.Color GetColorFromArgb(IList<byte> argb)
        {
            return System.Drawing.Color.FromArgb(argb[3], argb[0], argb[1], argb[2]);
        }

        public void SetLabelJsonValue<T>(string path, IList<T> value)
        {
            string[] paths = path.Split('.');

            JToken token = labelJson[paths[0]];
            for (int i = 1; i < paths.Length; i++)
            {
                token = token[paths[i]];
            }

            for (int i = 0; i < value.Count; i++)
            {
                (token as JArray)[i] = new JValue(value[i]);
            }

        }
        public void SetLabelJsonValue<T>(string path, T value)
        {
            string[] paths = path.Split('.');

            JToken token = labelJson[paths[0]];
            for (int i = 1; i < paths.Length; i++)
            {
                token = token[paths[i]];
            }

           (token as JValue).Value = value;

        }

        public T[] GetLabelJsonValue<T>(string path, IEnumerable<T> defaultValue)
        {
            try
            {
                string[] paths = path.Split('.');

                JToken token = labelJson[paths[0]];
                for (int i = 1; i < paths.Length; i++)
                {
                    token = token[paths[i]];
                }

                if (token is JArray value)
                {
                    if (value[0] is JValue)
                    {

                        return value.Select(p => (p as JValue).Value<T>()).ToArray();

                    }
                }
                return defaultValue.ToArray();
            }
            catch (Exception ex)
            {
                return defaultValue.ToArray();
            }
        }
        public T GetLabelJsonValue<T>(string path, T defaultValue)
        {
            try
            {
                string[] paths = path.Split('.');

                JToken token = labelJson[paths[0]];
                for (int i = 1; i < paths.Length; i++)
                {
                    token = token[paths[i]];
                }

                if (token is JValue value)
                {
                    return value.Value<T>();
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }


        //private bool useStatic = false;
        //public bool UseStatic
        //{
        //    get => useStatic;
        //    set => SetValueAndNotify(ref useStatic, value, nameof(UseStatic));
        //}


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

        private void CreateKeyButtonClick(object sender, RoutedEventArgs e)
        {
            InputDialog dialog = new InputDialog("请输入键值");
            if (dialog.ShowDialog() == true)
            {
                string key = dialog.Text;
                if (Keys.Any(p => p.Key == key))
                {
                    TaskDialog.ShowError("该键已存在");
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

        private void ChangeKeyButtonClick(object sender, RoutedEventArgs e)
        {
            InputDialog dialog = new InputDialog("请输入键值");
            if (dialog.ShowDialog() == true)
            {
                string key = dialog.Text;
                if (Keys.Any(p => p.Key == key))
                {
                    TaskDialog.ShowError("该键已存在");
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
            var keys = (await style.GetAllFeatures()).Select(p => p.GetAttributeValue(Resource.KeyFieldName) as string).Distinct();
            foreach (var key in keys)
            {
                if(Keys.Any(p=>p.Key==key) || key=="")
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

        Random r = new Random();
        private Color GetRandomColor()
        {

            int R = r.Next(255);
            int G = r.Next(255);
            int B = r.Next(255);
            B = (R + G > 400) ? R + G - 400 : B;//0 : 380 - R - G;
            B = (B > 255) ? 255 : B;
            return Color.FromArgb(255,R, G, B);
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
