using MapBoard.Main.Model;
using MapBoard.Main.UI.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using MapBoard.Main.Model.Extension;
using FzLib.Extension;
using Esri.ArcGISRuntime.Data;
using System.Diagnostics;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeTableDialog : Common.DialogWindowBase
    {
        private FeatureAttributes[] attributes;

        private bool close = false;

        private HashSet<FeatureAttributes> editedAttributes = new HashSet<FeatureAttributes>();

        private AttributeTableDialog(MapLayerInfo layer, ArcMapView mapView)
        {
            InitializeComponent();
            Title = "属性表 - " + layer.Name;
            Layer = layer;
            MapView = mapView;
            Width = 800;
            Height = 600;
        }

        public FeatureAttributes[] Attributes
        {
            get => attributes;
            private set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

        private static Dictionary<MapLayerInfo, AttributeTableDialog> dialogs = new Dictionary<MapLayerInfo, AttributeTableDialog>();

        public static AttributeTableDialog Get(MapLayerInfo layer, ArcMapView mapView)
        {
            if (dialogs.ContainsKey(layer))
            {
                return dialogs[layer];
            }
            var dialog = new AttributeTableDialog(layer, mapView);
            dialogs.Add(layer, dialog);
            return dialog;
        }

        public int EditedFeaturesCount => editedAttributes.Count;

        public MapLayerInfo Layer { get; }
        public ArcMapView MapView { get; }

        public bool IsLoaded { get; private set; } = false;

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <returns></returns>
        public async Task LoadAsync()
        {
            if (IsLoaded)
            {
                return;
            }
            IsLoaded = true;
            var features = await Layer.GetAllFeaturesAsync();
            Attributes = features.Select(p => FeatureAttributes.FromFeature(Layer, p)).ToArray();
            if (Attributes.Length == 0)
            {
                throw new Exception("没有任何要素");
            }
            var fields = Layer.Fields.IncludeDefaultFields().ToList();

            int column = 0;

            foreach (var field in Attributes[0].All)
            {
                string path = null;
                switch (field.Type)
                {
                    case FieldInfoType.Integer:
                        path = nameof(field.IntValue);
                        break;

                    case FieldInfoType.Float:
                        path = nameof(field.FloatValue);
                        break;

                    case FieldInfoType.Date:
                        path = nameof(field.DateValue);
                        break;

                    case FieldInfoType.Text:
                        path = nameof(field.TextValue);
                        break;
                }
                dg.Columns.Add(new DataGridTextColumn()
                {
                    Header = field.DisplayName,
                    Binding = new Binding($"All[{column}].{path}")
                    {
                        StringFormat = field.Type == FieldInfoType.Date ? "{0:yyyy-MM-dd}" : null
                    }
                });
                column++;
            }
            AddButton(dg, "缩放到图形", new RoutedEventHandler(LocateButton_Click));
            AddButton(dg, "选择", new RoutedEventHandler(SelectButton_Click));
            AddButton(dg, "加入选择", new RoutedEventHandler(AddSelectButton_Click));
        }

        /// <summary>
        /// 在表格末尾添加列按钮
        /// </summary>
        /// <param name="dg"></param>
        /// <param name="content"></param>
        /// <param name="handler"></param>
        private void AddButton(DataGrid dg, object content, Delegate handler)
        {
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(Button));
            //factory.SetValue(OpacityProperty, 0.7);
            factory.SetValue(ContentProperty, content);
            factory.SetResourceReference(ForegroundProperty, "SystemControlHighlightAccentBrush");
            factory.SetValue(BackgroundProperty, System.Windows.Media.Brushes.Transparent);
            factory.SetValue(TagProperty, new Binding("."));
            factory.AddHandler(Button.ClickEvent, handler);
            dg.Columns.Add(new DataGridTemplateColumn()
            {
                CellTemplate = new DataTemplate(typeof(FeatureAttributes))
                {
                    VisualTree = factory
                }
            });
        }

        private void AddSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var feature = ((sender as Button).Tag as FeatureAttributes).Feature;
            MapView.Selection.Select(feature);
        }

        private async void AttributeTableDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (close)
            {
                Debug.Assert(dialogs.ContainsKey(Layer));
                dialogs.Remove(Layer);
                return;
            }
            if (EditedFeaturesCount > 0)
            {
                e.Cancel = true;
                if (await CommonDialog.ShowYesNoDialogAsync("是否关闭", "当前编辑未保存，是否关闭？"))
                {
                    close = true;
                    Close();
                }
            }
            else
            {
                Debug.Assert(dialogs.ContainsKey(Layer));
                dialogs.Remove(Layer);
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            btnSave.IsEnabled = false;
            List<UpdatedFeature> features = new List<UpdatedFeature>();
            foreach (var attr in editedAttributes)
            {
                var oldAttrs = new Dictionary<string, object>(attr.Feature.Attributes);
                attr.SaveToFeature();
                features.Add(new UpdatedFeature(attr.Feature, attr.Feature.Geometry, oldAttrs));
            }
            await Layer.UpdateFeaturesAsync(features, FeaturesChangedSource.Edit);

            editedAttributes.Clear();
            this.Notify(nameof(EditedFeaturesCount));
        }

        private async void LocateButton_Click(object sender, RoutedEventArgs e)
        {
            var feature = ((sender as Button).Tag as FeatureAttributes).Feature;
            await MapView.ZoomToGeometryAsync(feature.Geometry);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var feature = ((sender as Button).Tag as FeatureAttributes).Feature;
            MapView.Selection.Select(feature, true);
        }

        private void Dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is FeatureAttributes attribute)
                {
                    editedAttributes.Add(attribute);
                    btnSave.IsEnabled = true;
                    this.Notify(nameof(EditedFeaturesCount));
                }
            }
        }
    }
}