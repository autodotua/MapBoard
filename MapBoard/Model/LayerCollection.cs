using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Extension;
using MapBoard.Common;
using MapBoard.Main.UI;
using MapBoard.Main.UI.Map;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FzLib.Basic.Loop;

namespace MapBoard.Main.Model
{
    public class LayerCollection : FzLib.DataStorage.Serialization.JsonSerializationBase, INotifyPropertyChanged
    {
        public static readonly string LayersFileName = "layers.json";
        private bool canSave = false;

        private static LayerCollection instance;

        public static LayerCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    var settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };

                    instance = TryOpenOrCreate<LayerCollection>(System.IO.Path.Combine(Config.DataPath, LayersFileName), settings);
                    if (instance.Layers.Count > 0)
                    {
                        var Layers = instance.Layers.ToArray();
                        instance.layers.Clear();
                        instance.layers.CollectionChanged += instance.LayersCollectionChanged;
                        Layers.ForEach(p => instance.Layers.Add(p));
                    }
                    else
                    {
                        instance.layers.CollectionChanged += instance.LayersCollectionChanged;
                    }

                    if (instance.SelectedIndex >= 0 && instance.SelectedIndex < instance.Layers.Count)
                    {
                        instance.Selected = instance.Layers[instance.SelectedIndex];
                    }
                    instance.SaveWhenChanged = true;
                }
                instance.canSave = true;
                return instance;
            }
        }

        public bool SaveWhenChanged { get; set; } = false;

        private ObservableCollection<LayerInfo> layers = new ObservableCollection<LayerInfo>();

        public ObservableCollection<LayerInfo> Layers
        {
            get => layers;
            set
            {
                layers = value;
                if (value != null)
                {
                    if (value.Count > 0)
                    {
                        value.ForEachAsync(async p => await ArcMapView.Instance.Layer.AddLayerAsync(p));
                    }
                }
            }
        }

        private LayerInfo selected;

        private async void LayersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        var layer = item as LayerInfo;
                        if (await ArcMapView.Instance.Layer.AddLayerAsync(layer))
                        {
                            layer.PropertyChanged += LayerPropertyChanged;
                        }
                        else
                        {
                            Layers.CollectionChanged -= instance.LayersCollectionChanged;
                            Layers.Remove(item as LayerInfo);
                            Layers.CollectionChanged += instance.LayersCollectionChanged;
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    ArcMapView.Instance.Map.OperationalLayers.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (LayerInfo layer in e.OldItems)
                    {
                        ArcMapView.Instance.Layer.RemoveLayer(layer);
                        layer.PropertyChanged -= LayerPropertyChanged;
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    ArcMapView.Instance.Layer.ClearLayers();
                    break;
            }
            if (SaveWhenChanged && canSave)
            {
                Save();
            }
        }

        public event EventHandler LayerVisibilityChanged;

        public override void Save()
        {
            SelectedIndex = Layers.IndexOf(Selected);

            if (new FileInfo(Path).Directory.Exists)
            {
                base.Save();
            }
        }

        public int SelectedIndex { get; set; }

        [JsonIgnore]
        public LayerInfo Selected
        {
            get => selected;
            set
            {
                selected = value;
                this.Notify();
            }
        }

        private void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerInfo.LayerVisible))
            {
                LayerVisibilityChanged?.Invoke(this, new EventArgs());
            }
            Save();
        }

        public static void ResetLayers()
        {
            instance.SaveWhenChanged = false;
            instance.Layers.Clear();
            instance = null;
            _ = Instance;
            LayerInstanceChanged?.Invoke(Instance, new EventArgs());
        }

        public static event EventHandler LayerInstanceChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public string MapViewExtentJson { get; set; }
    }
}