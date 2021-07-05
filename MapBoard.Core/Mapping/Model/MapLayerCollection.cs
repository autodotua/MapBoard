using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Basic;
using FzLib.Extension;
using MapBoard.Model;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MLayerCollection = MapBoard.Model.LayerCollection;
using ELayerCollection = Esri.ArcGISRuntime.Mapping.LayerCollection;

namespace MapBoard.Mapping.Model
{
    public class MapLayerCollection : MLayerCollection
    {
        public const string LayersFileName = "layers.json";

        private MapLayerInfo selected;

        private MapLayerCollection(ELayerCollection esriLayers)
        {
            EsriLayers = esriLayers;
            SetLayers(new ObservableCollection<LayerInfo>());
        }

        public MapLayerInfo Find(FeatureLayer layer)
        {
            return LayerList.Cast<MapLayerInfo>().FirstOrDefault(p => p.Layer == layer);
        }

        public MapLayerInfo Selected
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

        public static async Task<MapLayerCollection> GetInstanceAsync(ELayerCollection esriLayers)
        {
            string path = Path.Combine(Parameters.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                return new MapLayerCollection(esriLayers);
            }
            MapLayerCollection instance = null;
            await Task.Run(() =>
            instance = FromFile<MapLayerCollection, MapLayerInfo>(path,
            () => new MapLayerCollection(esriLayers)));
            List<string> errorMsgs = new List<string>();
            foreach (var layer in instance.LayerList.Cast<MapLayerInfo>().ToList())
            {
                try
                {
                    await instance.AddAsync(layer, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"图层{layer.Name}加载失败：{ex.Message}");
                    instance.LayerList.Remove(layer);
                }
            }

            if (instance.SelectedIndex >= 0
                && instance.SelectedIndex < instance.Count)
            {
                instance.Selected = instance[instance.SelectedIndex] as MapLayerInfo;
            }
            return instance;
        }

        public Task AddAsync(MapLayerInfo layer)
        {
            return AddAsync(layer, true);
        }

        public void Clear()
        {
            foreach (var layer in EsriLayers.ToArray())
            {
                EsriLayers.Remove(layer);
            }
            foreach (MapLayerInfo layer in LayerList)
            {
                layer.Dispose();
            }
            LayerList.Clear();
        }

        public async Task InsertAsync(int index, MapLayerInfo layer)
        {
            await AddLayerAsync(layer, Count - index);
            layer.PropertyChanged += OnLayerPropertyChanged;
            LayerList.Insert(index, layer);
        }

        public void Move(int fromIndex, int toIndex)
        {
            EsriLayers.Move(Count - fromIndex - 1, Count - toIndex - 1);
            LayerList.Move(fromIndex, toIndex);
        }

        public void Remove(MapLayerInfo layer)
        {
            try
            {
                EsriLayers.Remove(layer.Layer);
                layer.Dispose();
            }
            catch
            {
            }
            layer.PropertyChanged -= OnLayerPropertyChanged;
            LayerList.Remove(layer);
        }

        private async Task AddAsync(MapLayerInfo layer, bool addToCollection)
        {
            await AddLayerAsync(layer, 0);
            layer.PropertyChanged += OnLayerPropertyChanged;
            if (addToCollection)
            {
                LayerList.Add(layer);
            }
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        public void Save()
        {
            Save(Path.Combine(Parameters.DataPath, LayersFileName));
        }

        public ELayerCollection EsriLayers { get; }

        private async Task AddLayerAsync(MapLayerInfo layer, int index)
        {
            try
            {
                if (!layer.HasTable)
                {
                    await layer.SetTableAsync(new ShapefileFeatureTable(layer.GetFilePath()));
                }
                FeatureLayer fl = layer.Layer;
                if (index == -1)
                {
                    EsriLayers.Add(fl);
                }
                else
                {
                    EsriLayers.Insert(index, fl);
                }
                await Task.Run(layer.ApplyStyle);
                await layer.LayerCompleteAsync();
            }
            catch (Exception ex)
            {
                try
                {
                    layer.Dispose();
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
            if (!Directory.Exists(Parameters.DataPath))
            {
                Directory.CreateDirectory(Parameters.DataPath);
                return;
            }

            foreach (var layer in LayerList.Cast<MapLayerInfo>().ToList())
            {
                if (File.Exists(Path.Combine(Parameters.DataPath, layer.Name + ".shp")))
                {
                    await LoadLayerAsync(layer);
                }
                else
                {
                    Remove(layer);
                }
            }
            return;
            //下面的啥东西？
            HashSet<string> files = Directory.EnumerateFiles(Parameters.DataPath)
                .Where(p => Path.GetExtension(p) == ".shp")
                .Select(p =>
                {
                    int index = p.LastIndexOf('.');
                    if (index == -1)
                    {
                        return p;
                    }
                    return p.Remove(index, p.Length - index).RemoveStart(Parameters.DataPath + "\\");
                }).ToHashSet();

            foreach (var name in files)
            {
                if (!LayerList.Any(p => p.Name == name))
                {
                    MapLayerInfo style = new MapLayerInfo();
                    style.Name = name;
                    await LoadLayerAsync(style);
                }
            }
        }

        public async Task LoadLayerAsync(MapLayerInfo layer)
        {
            ShapefileFeatureTable featureTable = new ShapefileFeatureTable(Parameters.DataPath + "\\" + layer.Name + ".shp");
            await featureTable.LoadAsync();
            if (featureTable.LoadStatus == LoadStatus.Loaded)
            {
            }
        }
    }
}