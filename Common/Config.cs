﻿using Newtonsoft.Json;
using System;

namespace MapBoard.Common
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


        public string Url { get; set; } = "http://t0.tianditu.com/vec_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=vec&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=9396357d4b92e8e197eafa646c3c541d\r\nhttp://t0.tianditu.com/cva_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=cva&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=9396357d4b92e8e197eafa646c3c541d";
        [JsonIgnore]
        public string[] Urls => Url.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        [JsonIgnore]

        public static string DataPath { get; set; } = "Data";

        public bool StaticEnable { get; set; } = true;

        public double StaticWidth { get; set; } = 100;

        public bool GpxHeight { get; set; } = true;
        public double GpxHeightExaggeratedMagnification { get; set; } = 5;
        public bool GpxRelativeHeight { get; set; } = true;

        public override void Save()
        {
            base.Save();
        }

        public string BasemapCoordinateSystem { get; set; } = "CGCS2000";

        public bool RemainLabel { get; set; } = false;

        public bool GpxAutoSmooth { get; set; } = true;
        public bool GpxAutoSmoothOnlyZ { get; set; } = false;
        public int GpxAutoSmoothLevel { get; set; } = 5;


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
