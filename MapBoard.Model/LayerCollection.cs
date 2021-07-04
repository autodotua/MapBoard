using FzLib.Basic;
using FzLib.Extension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MapBoard.Model
{
    public class LayerCollection : IReadOnlyList<LayerInfo>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public class LayerPropertyChangedEventArgs : PropertyChangedEventArgs
        {
            public LayerPropertyChangedEventArgs(LayerInfo layer, string propertyName) : base(propertyName)
            {
                Layer = layer;
            }

            public LayerInfo Layer { get; set; }
        }

        protected ObservableCollection<LayerInfo> LayerList { get; private set; }

        protected void SetLayers(ObservableCollection<LayerInfo> layers)
        {
            Debug.Assert(layers != null);
            if (LayerList != null)
            {
                LayerList.CollectionChanged -= LayerList_CollectionChanged;
            }
            LayerList = layers;

            LayerList.CollectionChanged += LayerList_CollectionChanged;
        }

        private void LayerList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IEnumerable<LayerInfo> newLayers = null;
            IEnumerable<LayerInfo> oldLayers = null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    newLayers = e.NewItems.Cast<LayerInfo>();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    oldLayers = e.OldItems.Cast<LayerInfo>();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    oldLayers = e.OldItems.Cast<LayerInfo>();
                    newLayers = e.NewItems.Cast<LayerInfo>();
                    break;
            }
            if (newLayers != null)
            {
                newLayers.ForEach(p => p.PropertyChanged += Layer_PropertyChanged);
            }
            if (oldLayers != null)
            {
                oldLayers.ForEach(p => p.PropertyChanged -= Layer_PropertyChanged);
            }
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LayerPropertyChanged?.Invoke(this, new LayerPropertyChangedEventArgs(sender as LayerInfo, e.PropertyName));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                ((INotifyCollectionChanged)LayerList).CollectionChanged += value;
            }

            remove
            {
                ((INotifyCollectionChanged)LayerList).CollectionChanged -= value;
            }
        }

        public event EventHandler<LayerPropertyChangedEventArgs> LayerPropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count => LayerList.Count;

        private string mapViewExtentJson;

        public string MapViewExtentJson
        {
            get => mapViewExtentJson;
            set => this.SetValueAndNotify(ref mapViewExtentJson, value, nameof(MapViewExtentJson));
        }

        private int selectedIndex;

        public LayerCollection()
        {
        }

        public int SelectedIndex
        {
            get => selectedIndex;
            set => this.SetValueAndNotify(ref selectedIndex, value, nameof(SelectedIndex));
        }

        public LayerInfo this[int index] => LayerList[index];

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
            instance.SetLayers(new ObservableCollection<LayerInfo>());
            if (layersJson is JArray layerJsonArray)
            {
                foreach (var layerJson in layerJsonArray)
                {
                    instance.LayerList.Add(layerJson.ToObject<TLayer>());
                }
            }
            return instance;
        }

        public IEnumerator<LayerInfo> GetEnumerator()
        {
            return LayerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return LayerList.GetEnumerator();
        }

        public int IndexOf(LayerInfo layer)
        {
            return LayerList.IndexOf(layer);
        }

        public bool Contains(LayerInfo layer)
        {
            return LayerList.Contains(layer);
        }

        public void Save(string path)
        {
            JObject json = new JObject();
            json.Add(nameof(MapViewExtentJson), MapViewExtentJson);
            json.Add(nameof(SelectedIndex), SelectedIndex);
            json.Add("Layers", JArray.FromObject(LayerList));
            string jsonStr = json.ToString(Formatting.Indented);
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, jsonStr);
        }
    }
}