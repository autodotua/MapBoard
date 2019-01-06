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

namespace MapBoard.Code
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
                      instance.  Styles = JsonConvert.DeserializeObject<ObservableCollection<StyleInfo>>(File.ReadAllText(Path.Combine(Config.DataPath, "styles.json")));
                        if (instance.Styles==null)
                        {
                            instance.Styles = new ObservableCollection<StyleInfo>();
                        }
                    }
                    catch
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
        public ObservableCollection<StyleInfo> Styles { get; set; } = null;

        private StyleInfo selected;
        private StyleCollection()
        {
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
