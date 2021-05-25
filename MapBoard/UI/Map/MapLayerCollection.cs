﻿using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Basic;
using FzLib.Extension;
using FzLib.UI.Dialog;
using MapBoard.Common;
using MapBoard.Main.Model;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Main.UI.Map
{
    public class MapLayerCollection : Model.LayerCollection
    {
        public const string LayersFileName = "layers.json";

        private LayerInfo selected;

        private MapLayerCollection(Esri.ArcGISRuntime.Mapping.LayerCollection esriLayers)
        {
            EsriLayers = esriLayers;
            layers = new System.Collections.ObjectModel.ObservableCollection<LayerInfo>();
        }

        public LayerInfo Selected
        {
            get => selected;
            set
            {
                if (value != null)
                {
                    SelectedIndex = IndexOf(value);
                }
                else
                {
                    SelectedIndex = -1;
                }
                this.SetValueAndNotify(ref selected, value, nameof(Selected));
            }
        }

        public static async Task<MapLayerCollection> GetInstanceAsync(Esri.ArcGISRuntime.Mapping.LayerCollection esriLayers)
        {
            string path = Path.Combine(Config.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                return new MapLayerCollection(esriLayers);
            }
            var instance = FromFile(path, () => new MapLayerCollection(esriLayers));
            List<string> errorMsgs = new List<string>();
            foreach (var layer in instance.layers.ToList())
            {
                try
                {
                    await instance.AddAsync(layer, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"图层{layer.Name}加载失败：{ex.Message}");
                    instance.layers.Remove(layer);
                }
            }

            if (instance.SelectedIndex >= 0
                && instance.SelectedIndex < instance.Count)
            {
                instance.Selected = instance[instance.SelectedIndex];
            }
            return instance;
        }

        public Task AddAsync(LayerInfo layer)
        {
            return AddAsync(layer, true);
        }

        public void Clear()
        {
            foreach (var layer in EsriLayers.ToArray())
            {
                EsriLayers.Remove(layer);
                ((layer as FeatureLayer).FeatureTable as ShapefileFeatureTable).Close();
            }
            layers.Clear();
        }

        public async Task InsertAsync(int index, LayerInfo layer)
        {
            await AddLayerAsync(layer, index);
            layer.PropertyChanged += LayerPropertyChanged;
            layers.Insert(index, layer);
        }

        public void Move(int fromIndex, int toIndex)
        {
            EsriLayers.Move(fromIndex, toIndex);
            layers.Move(fromIndex, toIndex);
        }

        public void Remove(LayerInfo layer)
        {
            try
            {
                EsriLayers.Remove(layer.Layer);
                layer.Table.Close();
            }
            catch
            {
            }
            layer.PropertyChanged -= LayerPropertyChanged;
            layers.Remove(layer);
        }

        private async Task AddAsync(LayerInfo layer, bool addToCollection)
        {
            await AddLayerAsync(layer);
            layer.PropertyChanged += LayerPropertyChanged;
            if (addToCollection)
            {
                layers.Add(layer);
            }
        }

        public void Save()
        {
            Save(Path.Combine(Config.DataPath, LayersFileName));
        }

        protected override void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.LayerPropertyChanged(sender, e);
            Save();
        }

        public Esri.ArcGISRuntime.Mapping.LayerCollection EsriLayers { get; }

        private async Task AddLayerAsync(LayerInfo layer, int index = -1)
        {
            try
            {
                if (layer.Table == null)
                {
                    layer.Table = new ShapefileFeatureTable(layer.GetFileName());
                    await layer.Table.LoadAsync();
                }
                FeatureLayer fl = new FeatureLayer(layer.Table);
                if (index == -1)
                {
                    EsriLayers.Add(fl);
                }
                else
                {
                    EsriLayers.Insert(index, fl);
                }
                layer.ApplyStyle();
                await layer.LayerCompleteAsync();
            }
            catch (Exception ex)
            {
                try
                {
                    layer.Table.Close();
                    if (layer.Layer != null)
                    {
                        EsriLayers.Remove(layer.Layer);
                    }
                }
                catch
                {
                }
                throw;
            }
        }

        public async Task LoadLayersAsync()
        {
            if (!Directory.Exists(Config.DataPath))
            {
                Directory.CreateDirectory(Config.DataPath);
                return;
            }

            foreach (var layer in layers)
            {
                if (File.Exists(Path.Combine(Config.DataPath, layer.Name + ".shp")))
                {
                    await LoadLayerAsync(layer);
                }
                else
                {
                    Remove(layer);
                }
            }

            HashSet<string> files = Directory.EnumerateFiles(Config.DataPath)
                .Where(p => Path.GetExtension(p) == ".shp")
                .Select(p =>
                {
                    int index = p.LastIndexOf('.');
                    if (index == -1)
                    {
                        return p;
                    }
                    return p.Remove(index, p.Length - index).RemoveStart(Config.DataPath + "\\");
                }).ToHashSet();

            foreach (var name in files)
            {
                if (!layers.Any(p => p.Name == name))
                {
                    LayerInfo style = new LayerInfo();
                    style.Name = name;
                    await LoadLayerAsync(style);
                }
            }
        }

        public async Task LoadLayerAsync(LayerInfo layer)
        {
            try
            {
                ShapefileFeatureTable featureTable = new ShapefileFeatureTable(Config.DataPath + "\\" + layer.Name + ".shp");
                await featureTable.LoadAsync();
                if (featureTable.LoadStatus == LoadStatus.Loaded)
                {
                }
            }
            catch (Exception ex)
            {
                if (SnakeBar.DefaultOwner.Owner == null)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, $"无法加载图层{layer.Name}");
                }
                else
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, $"无法加载图层{layer.Name}");
                }
            }
        }
    }
}