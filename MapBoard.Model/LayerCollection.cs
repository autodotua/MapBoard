using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace MapBoard.Model
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
            return FromFile<LayerCollection, LayerInfo>(path, () => new LayerCollection());
        }

        public static TCollection FromFile<TCollection, TLayer>(string path, Func<TCollection> getInstance)
            where TCollection : LayerCollection
            where TLayer : LayerInfo
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
                    instance.layers.Add(layerJson.ToObject<TLayer>());
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

        public bool Contains(LayerInfo layer)
        {
            return layers.Contains(layer);
        }

        public void Save(string path)
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

        protected virtual void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerInfo.LayerVisible))
            {
                LayerVisibilityChanged?.Invoke(this, new EventArgs());
            }
        }
    }

    //public class LayerCollection<T> : IReadOnlyList<T>, INotifyPropertyChanged, INotifyCollectionChanged where T : LayerInfo
    // {
    //     protected ObservableCollection<T> layers;

    //     public event NotifyCollectionChangedEventHandler CollectionChanged
    //     {
    //         add
    //         {
    //             ((INotifyCollectionChanged)layers).CollectionChanged += value;
    //         }

    //         remove
    //         {
    //             ((INotifyCollectionChanged)layers).CollectionChanged -= value;
    //         }
    //     }

    //     public event EventHandler LayerVisibilityChanged;

    //     public event PropertyChangedEventHandler PropertyChanged;

    //     public int Count => layers.Count;

    //     public string MapViewExtentJson { get; set; }

    //     public int SelectedIndex { get; set; }
    //     public T this[int index] => layers[index];

    //     public static LayerCollection<T> FromFile(string path)
    //     {
    //         return FromFile(path, () => new LayerCollection<T>());
    //     }

    //     public static TC FromFile<TC>(string path, Func<TC> getInstance) where TC : LayerCollection<T>
    //     {
    //         var instance = getInstance();

    //         JObject json = JObject.Parse(File.ReadAllText(path));
    //         instance.MapViewExtentJson = json[nameof(MapViewExtentJson)]?.Value<string>();
    //         instance.SelectedIndex = json[nameof(SelectedIndex)]?.Value<int>() ?? 0;
    //         var layersJson = json["Layers"];
    //         instance.layers = new ObservableCollection<T>();
    //         if (layersJson is JArray layerJsonArray)
    //         {
    //             foreach (var layerJson in layerJsonArray)
    //             {
    //                 instance.layers.Add(layerJson.ToObject<T>());
    //             }
    //         }
    //         return instance;
    //     }

    //     public IEnumerator<T> GetEnumerator()
    //     {
    //         return layers.GetEnumerator();
    //     }

    //     IEnumerator IEnumerable.GetEnumerator()
    //     {
    //         return layers.GetEnumerator();
    //     }

    //     public int IndexOf(T layer)
    //     {
    //         return layers.IndexOf(layer);
    //     }

    //     public bool Contains(T layer)
    //     {
    //         return layers.Contains(layer);
    //     }

    //     public void Save(string path)
    //     {
    //         JObject json = new JObject();
    //         json.Add(nameof(MapViewExtentJson), MapViewExtentJson);
    //         json.Add(nameof(SelectedIndex), SelectedIndex);
    //         json.Add("Layers", JArray.FromObject(layers));
    //         string jsonStr = json.ToString(Formatting.Indented);
    //         string dir = Path.GetDirectoryName(path);
    //         if (!Directory.Exists(dir))
    //         {
    //             Directory.CreateDirectory(dir);
    //         }
    //         File.WriteAllText(path, jsonStr);
    //     }

    //     protected virtual void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
    //     {
    //         if (e.PropertyName == nameof(LayerInfo.LayerVisible))
    //         {
    //             LayerVisibilityChanged?.Invoke(this, new EventArgs());
    //         }
    //     }
    // }
}