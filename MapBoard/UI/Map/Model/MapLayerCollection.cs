using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Basic;
using FzLib.Extension;
using FzLib.WPF.Dialog;
using MapBoard.Common;
using MapBoard.Main.Model;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MLayerCollection = MapBoard.Main.Model.LayerCollection;
using ELayerCollection = Esri.ArcGISRuntime.Mapping.LayerCollection;

namespace MapBoard.Main.UI.Map.Model
{
    public class MapLayerCollection : MLayerCollection//, IReadOnlyList<MapLayerInfo>,IEnumerable<MapLayerInfo>
    {
        public const string LayersFileName = "layers.json";

        private MapLayerInfo selected;

        private MapLayerCollection(ELayerCollection esriLayers)
        {
            EsriLayers = esriLayers;
            layers = new ObservableCollection<LayerInfo>();
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
            MapLayerCollection instance = FromFile<MapLayerCollection, MapLayerInfo>(path, () => new MapLayerCollection(esriLayers));
            List<string> errorMsgs = new List<string>();
            foreach (var layer in instance.layers.Cast<MapLayerInfo>().ToList())
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
                ((layer as FeatureLayer).FeatureTable as ShapefileFeatureTable).Close();
            }
            layers.Clear();
        }

        public async Task InsertAsync(int index, MapLayerInfo layer)
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
            layer.PropertyChanged -= LayerPropertyChanged;
            layers.Remove(layer);
        }

        private async Task AddAsync(MapLayerInfo layer, bool addToCollection)
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
            Save(Path.Combine(Parameters.DataPath, LayersFileName));
        }

        protected override void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.LayerPropertyChanged(sender, e);
            Save();
        }

        public ELayerCollection EsriLayers { get; }

        //MapLayerInfo IReadOnlyList<MapLayerInfo>.this[int index] =>layers[index] as MapLayerInfo;

        private async Task AddLayerAsync(MapLayerInfo layer, int index = -1)
        {
            try
            {
                if (!layer.HasTable)
                {
                    await layer.SetTableAsync(new ShapefileFeatureTable(layer.GetFileName()));
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
                layer.ApplyStyle();
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

            foreach (var layer in layers.Cast<MapLayerInfo>().ToList())
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
                if (!layers.Any(p => p.Name == name))
                {
                    MapLayerInfo style = new MapLayerInfo();
                    style.Name = name;
                    await LoadLayerAsync(style);
                }
            }
        }

        public async Task LoadLayerAsync(MapLayerInfo layer)
        {
            try
            {
                ShapefileFeatureTable featureTable = new ShapefileFeatureTable(Parameters.DataPath + "\\" + layer.Name + ".shp");
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

        //IEnumerator<MapLayerInfo> IEnumerable<MapLayerInfo>.GetEnumerator()
        //{
        //    throw new NotImplementedException();
        //}
    }
}

//using Esri.ArcGISRuntime;
//using Esri.ArcGISRuntime.Data;
//using Esri.ArcGISRuntime.Mapping;
//using FzLib.Basic;
//using FzLib.Extension;
//using FzLib.WPF.Dialog;
//using MapBoard.Common;
//using MapBoard.Main.Model;
//using MapBoard.Main.Util;
//using ModernWpf.FzExtension.CommonDialog;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;

//namespace MapBoard.Main.UI.Map
//{
//    public class MapLayerCollection : LayerCollection
//    {
//        public const string LayersFileName = "layers.json";

//        private MapLayerInfo selected;

//        private MapLayerCollection(LayerCollection esriLayers)
//        {
//            EsriLayers = esriLayers;
//            layers = new ObservableCollection<MapLayerInfo>();
//        }

//        public MapLayerInfo Selected
//        {
//            get => selected;
//            set
//            {
//                if (value != null)
//                {
//                    SelectedIndex = IndexOf(value);
//                }
//                else
//                {
//                    SelectedIndex = -1;
//                }
//                this.SetValueAndNotify(ref selected, value, nameof(Selected));
//            }
//        }

//        public static async Task<MapLayerCollection> GetInstanceAsync(Esri.ArcGISRuntime.Mapping.LayerCollection esriLayers)
//        {
//            string path = Path.Combine(Parameters.DataPath, LayersFileName);
//            if (!File.Exists(path))
//            {
//                return new MapLayerCollection(esriLayers);
//            }
//            var instance = FromFile(path, () => new MapLayerCollection(esriLayers));
//            List<string> errorMsgs = new List<string>();
//            foreach (var layer in instance.layers.ToList())
//            {
//                try
//                {
//                    await instance.AddAsync(layer, false);
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"图层{layer.Name}加载失败：{ex.Message}");
//                    instance.layers.Remove(layer);
//                }
//            }

//            if (instance.SelectedIndex >= 0
//                && instance.SelectedIndex < instance.Count)
//            {
//                instance.Selected = instance[instance.SelectedIndex];
//            }
//            return instance;
//        }

//        public Task AddAsync(MapLayerInfo layer)
//        {
//            return AddAsync(layer, true);
//        }

//        public void Clear()
//        {
//            foreach (var layer in EsriLayers.ToArray())
//            {
//                EsriLayers.Remove(layer);
//                ((layer as FeatureLayer).FeatureTable as ShapefileFeatureTable).Close();
//            }
//            layers.Clear();
//        }

//        public async Task InsertAsync(int index, MapLayerInfo layer)
//        {
//            await AddLayerAsync(layer, index);
//            layer.PropertyChanged += LayerPropertyChanged;
//            layers.Insert(index, layer);
//        }

//        public void Move(int fromIndex, int toIndex)
//        {
//            EsriLayers.Move(fromIndex, toIndex);
//            layers.Move(fromIndex, toIndex);
//        }

//        public void Remove(MapLayerInfo layer)
//        {
//            try
//            {
//                EsriLayers.Remove(layer.Layer);
//                layer.Dispose();
//            }
//            catch
//            {
//            }
//            layer.PropertyChanged -= LayerPropertyChanged;
//            layers.Remove(layer);
//        }

//        private async Task AddAsync(MapLayerInfo layer, bool addToCollection)
//        {
//            await AddLayerAsync(layer);
//            layer.PropertyChanged += LayerPropertyChanged;
//            if (addToCollection)
//            {
//                layers.Add(layer);
//            }
//        }

//        public void Save()
//        {
//            Save(Path.Combine(Parameters.DataPath, LayersFileName));
//        }

//        protected override void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
//        {
//            base.LayerPropertyChanged(sender, e);
//            Save();
//        }

//        public LayerCollection EsriLayers { get; }

//        private async Task AddLayerAsync(MapLayerInfo layer, int index = -1)
//        {
//            try
//            {
//                if (!layer.HasTable)
//                {
//                    await layer.SetTableAsync(new ShapefileFeatureTable(layer.GetFileName()));
//                }
//                FeatureLayer fl = layer.Layer;
//                if (index == -1)
//                {
//                    EsriLayers.Add(fl);
//                }
//                else
//                {
//                    EsriLayers.Insert(index, fl);
//                }
//                layer.ApplyStyle();
//                await layer.LayerCompleteAsync();
//            }
//            catch (Exception ex)
//            {
//                try
//                {
//                    layer.Dispose();
//                    if (layer.Layer != null)
//                    {
//                        EsriLayers.Remove(layer.Layer);
//                    }
//                }
//                catch
//                {
//                }
//                throw;
//            }
//        }

//        public async Task LoadLayersAsync()
//        {
//            if (!Directory.Exists(Parameters.DataPath))
//            {
//                Directory.CreateDirectory(Parameters.DataPath);
//                return;
//            }

//            foreach (var layer in layers)
//            {
//                if (File.Exists(Path.Combine(Parameters.DataPath, layer.Name + ".shp")))
//                {
//                    await LoadLayerAsync(layer);
//                }
//                else
//                {
//                    Remove(layer);
//                }
//            }

//            HashSet<string> files = Directory.EnumerateFiles(Parameters.DataPath)
//                .Where(p => Path.GetExtension(p) == ".shp")
//                .Select(p =>
//                {
//                    int index = p.LastIndexOf('.');
//                    if (index == -1)
//                    {
//                        return p;
//                    }
//                    return p.Remove(index, p.Length - index).RemoveStart(Parameters.DataPath + "\\");
//                }).ToHashSet();

//            foreach (var name in files)
//            {
//                if (!layers.Any(p => p.Name == name))
//                {
//                    MapLayerInfo style = new MapLayerInfo();
//                    style.Name = name;
//                    await LoadLayerAsync(style);
//                }
//            }
//        }

//        public async Task LoadLayerAsync(MapLayerInfo layer)
//        {
//            try
//            {
//                ShapefileFeatureTable featureTable = new ShapefileFeatureTable(Parameters.DataPath + "\\" + layer.Name + ".shp");
//                await featureTable.LoadAsync();
//                if (featureTable.LoadStatus == LoadStatus.Loaded)
//                {
//                }
//            }
//            catch (Exception ex)
//            {
//                if (SnakeBar.DefaultOwner.Owner == null)
//                {
//                    await CommonDialog.ShowErrorDialogAsync(ex, $"无法加载图层{layer.Name}");
//                }
//                else
//                {
//                    await CommonDialog.ShowErrorDialogAsync(ex, $"无法加载图层{layer.Name}");
//                }
//            }
//        }
//    }
//}