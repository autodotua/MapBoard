﻿using MapBoard.Main.Model;
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
using System.Collections.ObjectModel;
using MapBoard.Common;
using MapBoard.Main.UI.Model;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeTableDialog : LayerDialogBase
    {
        private ObservableCollection<FeatureAttributeCollection> attributes;
        private Dictionary<long, FeatureAttributeCollection> feature2Attributes;

        private HashSet<FeatureAttributeCollection> editedAttributes = new HashSet<FeatureAttributeCollection>();

        private AttributeTableDialog(Window owner, MapLayerInfo layer, ArcMapView mapView) : base(owner, layer)
        {
            InitializeComponent();
            Title = "属性表 - " + layer.Name;

            MapView = mapView;
            Width = 800;
            Height = 600;
            mapView.BoardTaskChanged += MapView_BoardTaskChanged;
        }

        private void MapView_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            IsEnabled = e.NewTask != BoardTask.Draw;
        }

        public ObservableCollection<FeatureAttributeCollection> Attributes
        {
            get => attributes;
            private set => this.SetValueAndNotify(ref attributes, value, nameof(Attributes));
        }

        private static Dictionary<MapLayerInfo, AttributeTableDialog> dialogs = new Dictionary<MapLayerInfo, AttributeTableDialog>();

        public static AttributeTableDialog Get(Window owner, MapLayerInfo layer, ArcMapView mapView)
        {
            if (dialogs.ContainsKey(layer))
            {
                return dialogs[layer];
            }
            var dialog = new AttributeTableDialog(owner, layer, mapView);
            dialogs.Add(layer, dialog);
            return dialog;
        }

        public int EditedFeaturesCount => editedAttributes.Count;

        public ArcMapView MapView { get; }

        public bool isLoaded = false;

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <returns></returns>
        public async Task LoadAsync()
        {
            if (isLoaded)
            {
                return;
            }
            isLoaded = true;
            var features = await Layer.GetAllFeaturesAsync();
            //获取要素的属性列表
            await Task.Run(() =>
            {
                Attributes = new ObservableCollection<FeatureAttributeCollection>(
                    features.Select(p =>
                        FeatureAttributeCollection.FromFeature(Layer, p)));
                feature2Attributes = attributes.ToDictionary(p => p.Feature.GetFID());
            });
            //对已经选择的要素应用到属性表中（理论上，不该存在的吧）
            foreach (var attr in Attributes)
            {
                if (MapView.Selection.SelectedFeatureIDs.Contains(attr.Feature.GetFID()))
                {
                    attr.IsSelected = true;
                }
            }
            if (Attributes.Count == 0)
            {
                throw new Exception("没有任何要素");
            }
            Layer.FeaturesChanged += Layer_FeaturesChanged;

            //将属性加入DataGrid中
            var fields = Layer.Fields.IncludeDefaultFields().ToList();
            int column = 0;
            foreach (var field in Attributes[0].All)
            {
                string path = null;
                var binding = new Binding();

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
                        binding.StringFormat = Parameters.DateFormat;
                        break;

                    case FieldInfoType.Time:
                        path = nameof(field.TimeValue);
                        binding.StringFormat = Parameters.TimeFormat;
                        break;

                    case FieldInfoType.Text:
                        path = nameof(field.TextValue);
                        break;
                }
                binding.Path = new PropertyPath($"All[{column}].{path}");
                dg.Columns.Add(new DataGridTextColumn()
                {
                    Header = field.DisplayName,
                    Binding = binding,
                    IsReadOnly = field.Name == Parameters.CreateTimeFieldName
                });
                column++;
            }
            AddButton(dg, "缩放到图形", new RoutedEventHandler(LocateButton_Click));

            //地图的选择发生改变后，需要同步更新表格的选择
            MapView.Selection.CollectionChanged += Selection_CollectionChanged;
        }

        /// <summary>
        /// 图层中选择的要素修改后，同步更新属性表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Selection_CollectionChanged(object sender, Map.SelectedFeaturesChangedEventArgs e)
        {
            if (e.Layer != Layer)
            {
                return;
            }
            if (e.Selected.Length + e.UnSelected.Length == 0)
            {
                return;
            }
            foreach (var feature in e.Selected)
            {
                long id = feature.GetFID();
                if (feature2Attributes.ContainsKey(id))
                {
                    feature2Attributes[id].IsSelected = true;
                }
            }
            foreach (var feature in e.UnSelected)
            {
                long id = feature.GetFID();
                if (feature2Attributes.ContainsKey(id))
                {
                    feature2Attributes[id].IsSelected = false;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// 不知道什么原因，用DataGridCheckBoxColumn无法双向绑定IsSelected，
        /// 因此改用更灵活的方式，并且使用单向绑定+事件的方式更新数据
        /// </remarks>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var fa = (sender as FrameworkElement).Tag as FeatureAttributeCollection;
            Debug.Assert(fa != null);
            if ((sender as CheckBox).IsChecked == true)
            {
                MapView.Selection.Select(fa.Feature);
            }
            else
            {
                MapView.Selection.UnSelect(fa.Feature);
            }
        }

        private void Layer_FeaturesChanged(object sender, FeaturesChangedEventArgs e)
        {
            if (e.AddedFeatures != null)
            {
                foreach (var f in e.AddedFeatures)
                {
                    long fid = f.GetFID();
                    var attr = FeatureAttributeCollection.FromFeature(Layer, f);
                    Attributes.Add(attr);
                    feature2Attributes.Add(fid, attr);
                }
            }
            else if (e.DeletedFeatures != null)
            {
                foreach (var f in e.DeletedFeatures)
                {
                    long fid = f.GetFID();
                    if (feature2Attributes.ContainsKey(fid))
                    {
                        Attributes.Remove(feature2Attributes[fid]);
                        feature2Attributes.Remove(fid);
                    }
                    else
                    {
                    }
                }
            }
            else if (e.UpdatedFeatures != null)
            {
                foreach (var f in e.UpdatedFeatures)
                {
                    long fid = f.Feature.GetFID();
                    if (feature2Attributes.ContainsKey(fid))
                    {
                        int index = Attributes.IndexOf(feature2Attributes[fid]);
                        Attributes.RemoveAt(index);
                        var attr = FeatureAttributeCollection.FromFeature(Layer, f.Feature);
                        feature2Attributes[fid] = attr;
                        Attributes.Insert(index, attr);
                    }
                }
            }
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
                CellTemplate = new DataTemplate(typeof(FeatureAttributeCollection))
                {
                    VisualTree = factory
                }
            });
        }

        private void AddSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var feature = ((sender as Button).Tag as FeatureAttributeCollection).Feature;
            MapView.Selection.Select(feature);
        }

        private async void AttributeTableDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing)
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
                    closing = true;
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
            foreach (var attr in editedAttributes.Where(p => feature2Attributes.ContainsKey(p.Feature.GetFID())))
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
            var feature = ((sender as Button).Tag as FeatureAttributeCollection).Feature;
            await MapView.ZoomToGeometryAsync(feature.Geometry);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var feature = ((sender as Button).Tag as FeatureAttributeCollection).Feature;
            MapView.Selection.Select(feature, true);
        }

        private void Dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is FeatureAttributeCollection attribute)
                {
                    editedAttributes.Add(attribute);
                    btnSave.IsEnabled = true;
                    this.Notify(nameof(EditedFeaturesCount));
                }
            }
        }
    }
}