using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace MapBoard.Main.Model
{
    public class LayerCollection : IReadOnlyList<LayerInfo>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        protected ObservableCollection<LayerInfo> layers;

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

        public event EventHandler LayerVisibilityChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => layers.Count;

        public string MapViewExtentJson { get; set; }

        public int SelectedIndex { get; set; }
        public LayerInfo this[int index] => layers[index];

        public static LayerCollection FromFile(string path)
        {
            return FromFile(path, () => new LayerCollection());
        }

        public static T FromFile<T>(string path, Func<T> getInstance) where T : LayerCollection
        {
            var instance = getInstance();

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

        public void Save(string path = null)
        {
            JObject json = new JObject();
            json.Add(nameof(MapViewExtentJson), MapViewExtentJson);
            json.Add(nameof(SelectedIndex), SelectedIndex);
            json.Add("Layers", JArray.FromObject(layers));
            string jsonStr = json.ToString(Formatting.Indented);
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, jsonStr);
        }

        protected void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerInfo.LayerVisible))
            {
                LayerVisibilityChanged?.Invoke(this, new EventArgs());
            }
            Save();
        }
    }
}