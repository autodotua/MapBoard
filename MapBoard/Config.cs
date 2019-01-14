using FzLib.Control.Dialog;
using MapBoard.Style;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard
{
    public class Config : FzLib.DataStorage.Serialization.JsonSerializationBase
    {
        private static Config instance;

        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    // DataPath = "Data";
                    //try
                    //{
                    //    DataPath = File.ReadLines("DataPath.ini").First();
                    //}
                    //catch
                    //{

                    //}
                    //try
                    //{
                    //    instance = TryOpenOrCreate<Config>(System.IO.Path.Combine(DataPath,"config.json"));
                    //}
                    //catch
                    //{
                    //    DataPath = "Data";
                    //    TaskDialog.ShowError(null,"无法识别指定的数据目录，将使用默认的Data目录");
                    //    instance = TryOpenOrCreate<Config>();
                    //}
                    instance = TryOpenOrCreate<Config>(System.IO.Path.Combine("config.json"));
                    instance.Settings.Formatting = Formatting.Indented;
                }
                return instance;
            }
        }
        

        public string Url { get; set; } = "http://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&scl=1&style=8&x={x}&y={y}&z={z}";
        [JsonIgnore]
        public static string DataPath { get; set; } = "Data";

        public bool StaticEnable { get; set; } = true;

        public double StaticWidth { get; set; } = 100;

        public override void Save()
        {
            base.Save();
            StyleCollection.Instance.Save();
        }

        public bool GCJ02 { get; set; } = true;

        public bool RemainLabel { get; set; } = false;


        //public List<StyleInfo> ShapefileStyles { get; } = new List<StyleInfo>();

        //public void AddToShapefileStyles(StyleInfo style)
        //{
        //    if(ShapefileStyles.Any(p=>p.Name==style.Name))
        //    {
        //        ShapefileStyles.Remove(ShapefileStyles.First(p => p.Name == style.Name));
        //    }
        //    ShapefileStyles.Add(style);
        //}
    }
}
