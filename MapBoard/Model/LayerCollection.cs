using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Main.UI.Map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace MapBoard.Main.Model
{
    public class LayerCollection : IReadOnlyList<LayerInfo>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public const string LayersFileName = "layers.json";

        private static LayerCollection instance;

        private ObservableCollection<LayerInfo> layers;

        private LayerInfo selected;

        private LayerCollection()
        {
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                ((INotifyCollectionChanged)layers).CollectionChanged += value;
            }

            remove
            {
                ((INotifyCollectionChanged)layers).CollectionChanged -= value;
            }
        }

        public static event EventHandler LayerInstanceChanged;

        public event EventHandler LayerVisibilityChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public static LayerCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new Exception("请先调用LoadInstanceAsync方法初始化实例");
                }
                return instance;
            }
        }

        public static bool IsInstanceLoaded { get; private set; } = false;
        public int Count => layers.Count;

        public string MapViewExtentJson { get; set; }

        [JsonIgnore]
        public LayerInfo Selected
        {
            get => selected;
            set => this.SetValueAndNotify(ref selected, value, nameof(Selected));
        }

        public int SelectedIndex { get; set; }
        public LayerInfo this[int index] => layers[index];

        public static LayerCollection GetInstance(string path)
        {
            var instance = new LayerCollection();

            JObject json = JObject.Parse(File.ReadAllText(path));
            instance.MapViewExtentJson = json[nameof(MapViewExtentJson)]?.Value<string>();
            instance.SelectedIndex = json[nameof(SelectedIndex)]?.Value<int>() ?? 0;
            var layersJson = json["Layers"];
            instance.layers = new ObservableCollection<LayerInfo>();
            if (layersJson is JArray layerJsonArray)
            {
                foreach (var layerJson in layerJsonArray)
                {
                    instance.layers.Add(layerJson.ToObject<LayerInfo>());
                }
            }
            return instance;
        }

        public static async Task LoadInstanceAsync()
        {
            string path = Path.Combine(Config.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                instance = new LayerCollection();
                return;
            }
            instance = GetInstance(path);
            foreach (var layer in instance.layers)
            {
                await instance.AddAsync(layer, false);
            }

            if (instance.SelectedIndex >= 0
                && instance.SelectedIndex < instance.Count)
            {
                instance.Selected = instance[instance.SelectedIndex];
            }
            LayerInstanceChanged?.Invoke(Instance, new EventArgs());
            IsInstanceLoaded = true;
        }

        public async static Task ResetLayersAsync()
        {
            instance.Clear();
            await LoadInstanceAsync();
        }

        public Task<bool> AddAsync(LayerInfo layer)
        {
            return AddAsync(layer, true);
        }

        private async Task<bool> AddAsync(LayerInfo layer, bool addToCollection)
        {
            if (await ArcMapView.Instance.Layer.AddLayerAsync(layer))
            {
                layer.PropertyChanged += LayerPropertyChanged;
                if (addToCollection)
                {
                    layers.Add(layer);
                }
                return true;
            }
            return false;
        }

        public void Clear()
        {
            ArcMapView.Instance.Layer.ClearLayers();
            layers.Clear();
        }

        public IEnumerator<LayerInfo> GetEnumerator()
        {
            return layers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return layers.GetEnumerator();
        }

        public int IndexOf(LayerInfo layer)
        {
            return layers.IndexOf(layer);
        }

        public async Task<bool> InsertAsync(int index, LayerInfo layer)
        {
            if (await ArcMapView.Instance.Layer.AddLayerAsync(layer))
            {
                layer.PropertyChanged += LayerPropertyChanged;
                layers.Insert(index, layer);
                return true;
            }
            return false;
        }

        public void Move(int fromIndex, int toIndex)
        {
            ArcMapView.Instance.Map.OperationalLayers.Move(fromIndex, toIndex);
            layers.Move(fromIndex, toIndex);
        }

        public void Remove(LayerInfo layer)
        {
            ArcMapView.Instance.Layer.RemoveLayer(layer);
            layer.PropertyChanged -= LayerPropertyChanged;
            layers.Remove(layer);
        }

        public void Save(string path = null)
        {
            var settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            JObject json = new JObject();
            json.Add(nameof(MapViewExtentJson), MapViewExtentJson);
            json.Add(nameof(SelectedIndex), SelectedIndex);
            json.Add("Layers", JArray.FromObject(layers));
            string jsonStr = json.ToString();
            if (path == null)
            {
                path = Path.Combine(Config.DataPath, LayersFileName);
            }
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, jsonStr);
        }

        private void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerInfo.LayerVisible))
            {
                LayerVisibilityChanged?.Invoke(this, new EventArgs());
            }
            Save();
        }
    }
}