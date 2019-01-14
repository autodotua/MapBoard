using Esri.ArcGISRuntime.Mapping;
using MapBoard.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FzLib.Basic.Collection.Loop;

namespace MapBoard.Style
{
    public class StyleCollection : FzLib.DataStorage.Serialization.JsonSerializationBase, INotifyPropertyChanged
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
                        value.ForEach(p => ArcMapView.Instance.AddLayer(p));
                    }
                }
            }
        }

        private StyleInfo selected;

        public event PropertyChangedEventHandler PropertyChanged;



        private void StylesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    e.NewItems.ForEach(p => ArcMapView.Instance.AddLayer(p as StyleInfo));
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    ArcMapView.Instance.Map.OperationalLayers.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    e.OldItems.ForEach(p => ArcMapView.Instance.RemoveLayer(p as StyleInfo));
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    ArcMapView.Instance.ClearLayers();
                    break;

            }
        }
        public override void Save()
        {
            SelectedIndex = Styles.IndexOf(Selected);
            base.Save();
        }
        public int SelectedIndex { get; set; }
        [JsonIgnore]
        public StyleInfo Selected
        {
            get => selected;
            set
            {

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
                //Notify(nameof(Selected));
            }
        }

        public static void ResetStyles()
        {
            instance.Styles.Clear();
            instance = null;
            var useless = Instance;

        }



        //public event EventHandler SelectionChanged;
    }
}
