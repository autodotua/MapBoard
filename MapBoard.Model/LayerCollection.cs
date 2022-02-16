using FzLib;
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
    public class LayerCollection : IReadOnlyList<ILayerInfo>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public LayerCollection()
        {
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

        public string MapViewExtentJson { get; set; }

        public int SelectedIndex { get; set; }

        protected ObservableCollection<ILayerInfo> LayerList { get; private set; }

        public ILayerInfo this[int index] => LayerList[index];

        public static LayerCollection FromFile(string path)
        {
            var instance = new LayerCollection();

            JObject json = JObject.Parse(File.ReadAllText(path));
            instance.MapViewExtentJson = json[nameof(MapViewExtentJson)]?.Value<string>();
            instance.SelectedIndex = json[nameof(SelectedIndex)]?.Value<int>() ?? 0;
            var layersJson = json["Layers"];
            instance.SetLayers(new ObservableCollection<ILayerInfo>());
            if (layersJson is JArray layerJsonArray)
            {
                foreach (var layerJson in layerJsonArray)
                {
                    instance.LayerList.Add(layerJson.ToObject<LayerInfo>());
                }
            }
            return instance;
        }

        public bool Contains(ILayerInfo layer)
        {
            return LayerList.Contains(layer);
        }

        public IEnumerator<ILayerInfo> GetEnumerator()
        {
            return LayerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return LayerList.GetEnumerator();
        }

        public int IndexOf(ILayerInfo layer)
        {
            return LayerList.IndexOf(layer);
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

        protected void SetLayers(ObservableCollection<ILayerInfo> layers)
        {
            Debug.Assert(layers != null);
            if (LayerList != null)
            {
                LayerList.CollectionChanged -= LayerList_CollectionChanged;
            }
            LayerList = layers;

            LayerList.CollectionChanged += LayerList_CollectionChanged;
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LayerPropertyChanged?.Invoke(this, new LayerPropertyChangedEventArgs(sender as LayerInfo, e.PropertyName));
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

        public class LayerPropertyChangedEventArgs : PropertyChangedEventArgs
        {
            public LayerPropertyChangedEventArgs(ILayerInfo layer, string propertyName) : base(propertyName)
            {
                Layer = layer;
            }

            public ILayerInfo Layer { get; set; }
        }
    }
}