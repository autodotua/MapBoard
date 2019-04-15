using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
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

namespace MapBoard.Main.Style
{
    public class StyleCollection : FzLib.DataStorage.Serialization.JsonSerializationBase
    {
        //private static StyleCollection instance = new StyleCollection();
        //public static StyleCollection Instance
        //{
        //    get
        //    {
        //        if (instance.Styles == null)
        //        {
        //            try
        //            {
        //                instance.Styles = JsonConvert.DeserializeObject<ObservableCollection<StyleInfo>>(File.ReadAllText(Path.Combine(Config.DataPath, "styles.json")));
        //                if (instance.Styles.Count > 0)
        //                {
        //                    instance.Selected = instance.Styles[0];
        //                }
        //                else if (instance.Styles == null)
        //                {
        //                    instance.Styles = new ObservableCollection<StyleInfo>();
        //                }
        //            }
        //            catch(Exception ex)
        //            {
        //                instance.Styles = new ObservableCollection<StyleInfo>();
        //            }
        //        }
        //        return instance;
        //    }
        //}

        private static StyleCollection instance;
        public static StyleCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = TryOpenOrCreate<StyleCollection>(System.IO.Path.Combine(Config.DataPath, "styles.json"));
                    if (instance.Styles.Count > 0)
                    {
                        var styles = instance.Styles.ToArray();
                        instance.styles.Clear();
                        instance.styles.CollectionChanged += instance.StylesCollectionChanged;
                        styles.ForEach(p => instance.Styles.Add(p));
                    }
                    else
                    {
                        instance.styles.CollectionChanged += instance.StylesCollectionChanged;
                    }

                    if (instance.SelectedIndex >= 0 && instance.SelectedIndex < instance.Styles.Count)
                    {
                        instance.Selected = instance.Styles[instance.SelectedIndex];
                    }
                    instance.Settings.Formatting = Formatting.Indented;
                    instance.canSave = true;
                    //try
                    //{
                    //    instance = JsonConvert.DeserializeObject<StyleCollection>(File.ReadAllText(Path.Combine(Config.DataPath, "styles.json")));
                    //    if (instance.Styles == null)
                    //    {
                    //        instance.Styles = new ObservableCollection<StyleInfo>();
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    instance.Styles = new ObservableCollection<StyleInfo>();
                    //}
                }
                return instance;
            }
        }
        private bool canSave = false;
        //public void Save()
        //{
        //    File.WriteAllText(Path.Combine(Config.DataPath, "styles.json"), JsonConvert.SerializeObject(Styles));
        //}
        private ObservableCollection<StyleInfo> styles = new ObservableCollection<StyleInfo>();
        public ObservableCollection<StyleInfo> Styles
        {
            get => styles;
            set
            {
                styles = value;
                if (value != null)
                {

                    if (value.Count > 0)
                    {
                        value.ForEachAsync(async p => await ArcMapView.Instance.Layer.AddLayerAsync(p));
                    }
                }
            }
        }

        private StyleInfo selected;
        private (double X1, double X2, double Y1, double Y2)? lastViewpointGeometry;

        private async void StylesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (!await ArcMapView.Instance.Layer.AddLayerAsync(item as StyleInfo))
                        {
                            Styles.CollectionChanged -= instance.StylesCollectionChanged;
                            Styles.Remove(item as StyleInfo);
                            Styles.CollectionChanged += instance.StylesCollectionChanged;
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    ArcMapView.Instance.Map.OperationalLayers.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    e.OldItems.ForEach(p => ArcMapView.Instance.Layer.RemoveLayer(p as StyleInfo));
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    ArcMapView.Instance.Layer.ClearLayers();
                    break;
            }
            if(canSave)
            {
                Save();
            }
        }

        public event EventHandler SelectedStyleVisibilityChanged;
        public override void Save()
        {
            SelectedIndex = Styles.IndexOf(Selected);
            Envelope envelope = ArcMapView.Instance.GetCurrentViewpoint(ViewpointType.BoundingGeometry)?.TargetGeometry as Envelope;
            if(envelope!=null)
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
        public StyleInfo Selected
        {
            get => selected;
            set
            {
                if (selected != null)
                {
                    selected.PropertyChanged += SelectedLayerPropertyChanged;
                }
                if (value != null)
                {
                    value.PropertyChanged += SelectedLayerPropertyChanged;
                }
                selected = value;
                //if (value != null)
                //{
                //    selected.CopyStyleFrom(Config.Instance.ShapefileStyles.First(p => p.Name == value.Name));
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
                Notify(nameof(Selected));
                //Notify(nameof(Selected));
            }
        }

        private void SelectedLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LayerVisible")
            {
                SelectedStyleVisibilityChanged?.Invoke(this, new EventArgs());
            }
        }

        public static void ResetStyles()
        {
            instance.canSave = false;
            instance.Styles.Clear();
            instance = null;
            var useless = Instance;

        }

        public (double X1,double X2,double Y1,double Y2)? LastViewpointGeometry {
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
