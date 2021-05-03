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

namespace MapBoard.Main.Layer
{
    public class LayerCollection : FzLib.DataStorage.Serialization.JsonSerializationBase, INotifyPropertyChanged
    {
        //private static LayerCollection instance = new LayerCollection();
        //public static LayerCollection Instance
        //{
        //    get
        //    {
        //        if (instance.Layers == null)
        //        {
        //            try
        //            {
        //                instance.Layers = JsonConvert.DeserializeObject<ObservableCollection<LayerInfo>>(File.ReadAllText(Path.Combine(Config.DataPath, "Layers.json")));
        //                if (instance.Layers.Count > 0)
        //                {
        //                    instance.Selected = instance.Layers[0];
        //                }
        //                else if (instance.Layers == null)
        //                {
        //                    instance.Layers = new ObservableCollection<LayerInfo>();
        //                }
        //            }
        //            catch(Exception ex)
        //            {
        //                instance.Layers = new ObservableCollection<LayerInfo>();
        //            }
        //        }
        //        return instance;
        //    }
        //}
        public static readonly string LayersFileName = "layers.json";

        private static LayerCollection instance;

        public static LayerCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = TryOpenOrCreate<LayerCollection>(System.IO.Path.Combine(Config.DataPath, LayersFileName));
                    instance.Settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
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
                    //try
                    //{
                    //    instance = JsonConvert.DeserializeObject<LayerCollection>(File.ReadAllText(Path.Combine(Config.DataPath, "Layers.json")));
                    //    if (instance.Layers == null)
                    //    {
                    //        instance.Layers = new ObservableCollection<LayerInfo>();
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    instance.Layers = new ObservableCollection<LayerInfo>();
                    //}
                }
                return instance;
            }
        }

        public bool SaveWhenChanged { get; set; } = false;

        //public void Save()
        //{
        //    File.WriteAllText(Path.Combine(Config.DataPath, "Layers.json"), JsonConvert.SerializeObject(Layers));
        //}
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
        private (double X1, double X2, double Y1, double Y2)? lastViewpointGeometry;

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
            if (SaveWhenChanged)
            {
                Save();
            }
        }

        public event EventHandler StyleVisibilityChanged;

        public override void Save()
        {
            SelectedIndex = Layers.IndexOf(Selected);
            Envelope envelope = ArcMapView.Instance.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry as Envelope;
            if (envelope != null)
            {
                lastViewpointGeometry = (envelope.XMin, envelope.XMax, envelope.YMax, envelope.YMin);
            }
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
                //if (selected != null)
                //{
                //    selected.PropertyChanged += SelectedLayerPropertyChanged;
                //}
                //if (value != null)
                //{
                //    value.PropertyChanged += SelectedLayerPropertyChanged;
                //}
                selected = value;
                //if (value != null)
                //{
                //    selected.CopyStyleFrom(Config.Instance.ShapefileLayers.First(p => p.Name == value.Name));
                //    //if(value.FeatureCount>0)
                //    //{
                //    //    method();
                //    //    //这里写一个本地方法是因为属性无法调用异步方法，
                //    //    //SetViewpointGeometryAsync方法的参数必须使用await
                //    //    async void method()
                //    //    {
                //    //        await Mapview.SetViewpointGeometryAsync(await value.Table.QueryExtentAsync(new Esri.ArcGISRuntime.Data.QueryParameters()));
                //    //    }
                //    //}
                //}
                //SelectionChanged?.Invoke(this, new EventArgs());

                this.Notify();
                //Notify(nameof(Selected));
            }
        }

        private void LayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerInfo.LayerVisible))
            {
                StyleVisibilityChanged?.Invoke(this, new EventArgs());
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

        public (double X1, double X2, double Y1, double Y2)? LastViewpointGeometry
        {
            get => lastViewpointGeometry;
            set
            {
                if (lastViewpointGeometry == null)
                {
                    if (value.HasValue)
                    {
                        ArcMapView.Instance.SetViewpointGeometryAsync(new Envelope(value.Value.X1, value.Value.Y1, value.Value.X2, value.Value.Y2));
                    }
                }
            }
        }

        //public event EventHandler SelectionChanged;
    }
}