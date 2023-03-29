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
using Esri.ArcGISRuntime.Data;
using FzLib.WPF.Converters;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MapBoard.UI
{
    public class SelectableObject<T> : INotifyPropertyChanged
    {
        public bool IsSelected { get; set; }
        public T ObjectData { get; set; }

        public SelectableObject(T objectData)
        {
            ObjectData = objectData;
        }

        public SelectableObject(T objectData, bool isSelected)
        {
            IsSelected = isSelected;
            ObjectData = objectData;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    /// <summary>
    /// RendererSettingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LayerSettingPanel : UserControlBase
    {
        private const string defaultKeyName = "（默认）";

        private Random r = new Random();
        private List<SelectableObject<FieldInfo>> keyFields;

        public LayerSettingPanel()
        {
            InitializeComponent();
            Fonts = FontFamily.FamilyNames.Values.ToArray();
        }

        public bool IsChangeOrDeleteKeyButtonEnabled => SelectedKey != null && SelectedKey.Key != defaultKeyName;
        public bool IsAddKeyButtonEnabled => KeyFields!=null && KeyFields.Any(p => p.IsSelected);
        public string[] Fonts { get; }

        [AlsoNotifyFor(nameof(KeyFieldsComboBoxText))]
        public List<SelectableObject<FieldInfo>> KeyFields
        {
            get => keyFields;
            private set
            {
                keyFields = value;
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        item.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(SelectableObject<FieldInfo>.IsSelected))
                            {
                                this.Notify(nameof(IsAddKeyButtonEnabled));
                            }
                        };
                    }
                }
            }
        }

        public ObservableCollection<KeySymbolPair> Keys { get; private set; } = new ObservableCollection<KeySymbolPair>();

        public LabelInfo Label { get; set; }

        public ObservableCollection<LabelInfo> Labels { get; set; }

        public string LayerName { get; set; } = "图层名称";

        public MapLayerCollection Layers => MapView?.Layers;

        [AlsoNotifyFor(nameof(Layers))]
        public MainMapView MapView { get; set; }

        [AlsoNotifyFor(nameof(IsChangeOrDeleteKeyButtonEnabled))]
        public KeySymbolPair SelectedKey { get; set; }

        public void Initialize(MainMapView mapView)
        {
            MapView = mapView;
            ResetLayerSettingUI();
            Layers.LayerPropertyChanged += Layers_LayerPropertyChanged;
            Layers.PropertyChanged += Layers_PropertyChanged;
        }

        /// <summary>
        /// 初始化所有Key，恢复只有默认Key的初始状态
        /// </summary>
        /// <param name="canKeepDefaultKey"></param>
        private void InitializeKeys(bool canKeepDefaultKey)
        {
            Debug.Assert(Layers.Selected != null);
            KeySymbolPair defaultSymbol = null;
            if (canKeepDefaultKey)
            {
                defaultSymbol = Keys.FirstOrDefault(p => p.Key == defaultKeyName)//优先级1：本来的默认
                ?? new KeySymbolPair(defaultKeyName, Layers.Selected.Renderer.DefaultSymbol//优先级2：定义的默认
                ?? Layers.Selected.GetDefaultSymbol());//优先级3：类型默认
            }
            else
            {
                defaultSymbol = new KeySymbolPair(defaultKeyName, Layers.Selected.Renderer.DefaultSymbol ?? Layers.Selected.GetDefaultSymbol());
            }
            Keys.Clear();
            Keys.Add(defaultSymbol);
            SelectedKey = Keys[0];
        }

        /// <summary>
        /// 配置=>UI
        /// </summary>
        public void ResetLayerSettingUI()
        {
            IMapLayerInfo layer = Layers.Selected;
            if (layer == null)
            {
                KeyFields = null;
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
                InitializeKeys(false);
                foreach (var symbol in layer.Renderer.Symbols)
                {
                    Keys.Add(new KeySymbolPair(symbol.Key, symbol.Value));
                }
                SelectedKey = Keys.First(p => p.Key == defaultKeyName);
                var keys = layer.Renderer.KeyFieldName?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

                KeyFields = layer?.Fields
                    ?.Where(p => p.CanBeRendererKey())
                    ?.Select(p => new SelectableObject<FieldInfo>(p, keys.Contains(p.Name)))
                    .ToList();

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

        /// <summary>
        /// UI=>配置
        /// </summary>
        /// <returns></returns>
        public async Task SetStyleFromUI()
        {
            var layer = Layers.Selected;

            layer.Renderer.Symbols.Clear();
            layer.Renderer.KeyFieldName = string.Join('|', KeyFields.Where(p => p.IsSelected).Select(p => p.ObjectData.Name));
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
            var key = await CommonDialog.ShowInputDialogAsync("请输入分类名"
                + (KeyFields.Count(p => p.IsSelected) > 1 ? "，多个字段的分类名之间使用“|”隔开" : ""),
                SelectedKey.Key);
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

        private void ClearKeyFieldButton_Click(object sender, RoutedEventArgs e)
        {
            KeyFields.ForEach(p => p.IsSelected = false);
            InitializeKeys(true);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (sender as ComboBox).SelectedItem = null;
        }

        private async void CreateKeyButtonClick(object sender, RoutedEventArgs e)
        {
            var key = await CommonDialog.ShowInputDialogAsync("请输入分类名"
                                + (KeyFields.Count(p => p.IsSelected) > 1 ? "，多个字段的分类名之间使用“|”隔开" : ""));
            if (key != null)
            {
                if (Keys.Any(p => p.Key == key))
                {
                    await CommonDialog.ShowErrorDialogAsync("该分类已存在");
                    return;
                }
                //try
                //{
                //    switch (KeyField.Type)
                //    {
                //        case FieldInfoType.Integer when !int.TryParse(key, out _):
                //        case FieldInfoType.Float when !double.TryParse(key, out _):
                //        case FieldInfoType.Date when !DateTime.TryParse(key, out _):
                //            throw new FormatException();
                //        case FieldInfoType.Time:
                //            //if (DateTime.TryParseExact(key, Parameters.TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time))
                //            //{
                //            //    key = time.ToString(Parameters.TimeFormat);
                //            //}
                //            //else if (DateTime.TryParse(key, out DateTime time2))
                //            //{
                //            //    key = time2.ToString(Parameters.TimeFormat);
                //            //}
                //            //else
                //            //{
                //            //    throw new FormatException();
                //            //}
                //            throw new Exception("无法使用时间类型作为Key");
                //    }
                //}
                //catch (FormatException)
                //{
                //    await CommonDialog.ShowErrorDialogAsync($"{key} 无法转换为字段 {KeyField.DisplayName} 的类型：{DescriptionConverter.GetDescription(KeyField.Type)}");
                //    return;
                //}

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

        /// <summary>
        /// 生成多个集合的笛卡尔积，由New Bing编写
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequences"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            // base case: 
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };
            foreach (var sequence in sequences)
            {
                // don't close over the loop variable (fixed in C# 5 BTW):
                var s = sequence;
                // recursive case: use SelectMany to build the new product out of the old one
                result =
                    from seq in result
                    from item in s
                    select seq.Concat(new[] { item });
            }
            return result;
        }

        /// <summary>
        /// 自动生成全部
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GenerateKeyButtonClick(object sender, RoutedEventArgs e)
        {
            var layer = Layers.Selected;
            Debug.Assert(KeyFields.Any(p => p.IsSelected));
            InitializeKeys(true);

            //生成每个字段的所有出现的属性值
            List<List<string>> keyses = new List<List<string>>();
            foreach (var key in KeyFields.Where(p => p.IsSelected))
            {
                var keysOfThisField = (await layer.GetUniqueAttributeValues(key.ObjectData.Name))
                    .Select(p => p.ToString())
                    .ToList();
                for (int i = 0; i < keysOfThisField.Count; i++)
                {
                    if (keysOfThisField[i] == "")
                    {
                        keysOfThisField[i] = "（空）";
                    }
                }
                keyses.Add(keysOfThisField.Select(p => p.ToString()).ToList());
            }

            //生成笛卡尔积
            int count = keyses.Aggregate(1, (p, l) => p * l.Count);
            if (count > 100)
            {
                await CommonDialog.ShowErrorDialogAsync("唯一值的组合超过了100项，请手动设置");
                return;
            }
            var keys = CartesianProduct(keyses).Select(x => string.Join("|", x)).ToList();

            //分配Symbol
            foreach (var key in keys)
            {
                if (!Keys.Any(p => p.Key == key.ToString()))
                {
                    SymbolInfo symbol = layer.GetDefaultSymbol();
                    Keys.Add(new KeySymbolPair(key.ToString(), symbol));
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
                    txtExpression.SelectedText = $"$feature.{(s as MenuItem).Tag as string}";
                    txtExpression.SelectionStart = txtExpression.SelectionStart + txtExpression.SelectionLength;
                };
                menuFields.Items.Add(item);
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

        private void KeyFieldCheckBox_CheckedOrUnchecked(object sender, RoutedEventArgs e)
        {
            this.Notify(nameof(KeyFieldsComboBoxText));
        }

        public string KeyFieldsComboBoxText => KeyFields == null ? "（未设置）" : string.Join(", ", KeyFields.Where(p => p.IsSelected).Select(p => p.ObjectData.DisplayName));
    }
}