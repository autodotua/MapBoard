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
    public class StyleCollection : FzLib.Extension.ExtendedINotifyPropertyChanged
    {
        private static StyleCollection instance = new StyleCollection();
        public static StyleCollection Instance
        {
            get
            {
                if (instance.Styles == null)
                {
                    try
                    {
                        instance.Styles = JsonConvert.DeserializeObject<ObservableCollection<StyleInfo>>(File.ReadAllText(Path.Combine(Config.DataPath, "styles.json")));
                        if (instance.Styles.Count > 0)
                        {
                            instance.Selected = instance.Styles[0];
                        }
                        else if (instance.Styles == null)
                        {
                            instance.Styles = new ObservableCollection<StyleInfo>();
                        }
                    }
                    catch(Exception ex)
                    {
                        instance.Styles = new ObservableCollection<StyleInfo>();
                    }
                }
                return instance;
            }
        }
        public void Save()
        {
            File.WriteAllText(Path.Combine(Config.DataPath, "styles.json"), JsonConvert.SerializeObject(Styles));
        }
        private ObservableCollection<StyleInfo> styles;
        public ObservableCollection<StyleInfo> Styles
        {
            get => styles;
            set
            {
                styles = value;
                if(value!=null)
                {
                    styles.CollectionChanged += StylesCollectionChanged;
                    if(value.Count>0)
                    {
                        value.ForEach(p => ArcMapView.Instance.AddLayer(p));
                    }
                }
            }
        }

        private StyleInfo selected;
        private StyleCollection()
        {
        }

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

                Notify(nameof(Selected));
            }
        }

        public StyleInfo Current => Selected ?? Config.Instance.DefaultStyle;



        //public event EventHandler SelectionChanged;
    }
}
