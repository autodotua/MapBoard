using FzLib.Extension;
using MapBoard.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;

namespace MapBoard
{
    public class Config : FzLib.DataStorage.Serialization.JsonSerializationBase, INotifyPropertyChanged
    {
        private static Config instance;

        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = TryOpenOrCreate<Config>(System.IO.Path.Combine(FzLib.Program.App.ProgramDirectoryPath, Parameters.ConfigPath));
                    instance.Settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
                }
                return instance;
            }
        }

        public override void Save()
        {
            base.Save();
        }

        private List<BaseLayerInfo> baseLayers = new List<BaseLayerInfo>();

        public List<BaseLayerInfo> BaseLayers
        {
            get => baseLayers;
            set => this.SetValueAndNotify(ref baseLayers, value, nameof(BaseLayers));
        }

        public CoordinateSystem BasemapCoordinateSystem { get; set; } = CoordinateSystem.WGS84;
        public bool HideWatermark { get; set; } = true;
        public static int WatermarkHeight = 72;
        public bool RemainLabel { get; set; } = false;
        public bool RemainKey { get; set; } = false;
        public bool RemainDate { get; set; } = false;
        public bool BackupWhenExit { get; set; } = true;
        public bool BackupWhenReplace { get; set; } = true;
        public int MaxBackupCount { get; set; } = 100;
        public double MaxScale { get; set; } = 100;
        public bool Gpx_AutoSmooth { get; set; } = true;
        public bool Gpx_AutoSmoothOnlyZ { get; set; } = false;
        public bool Gpx_Height { get; set; } = false;
        public bool Gpx_RelativeHeight { get; set; } = false;
        public int Gpx_AutoSmoothLevel { get; set; } = 5;
        public int Gpx_HeightExaggeratedMagnification { get; set; } = 5;
        public bool CopyShpFileWhenExport { get; set; } = true;
        private int theme = 0;

        public int Theme
        {
            get => theme;
            set
            {
                theme = value;
                ThemeChanged?.Invoke(this, new EventArgs());
            }
        }

        public string Tile_UserAgent { get; set; } = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";

        public event EventHandler ThemeChanged;

        public TileSourceCollection Tile_Urls { get; set; } = new TileSourceCollection();
        /*    public string Url { get; set; } = "http://mt3.google.cn/vt/lyrs=s&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}";
         高德地图： "http://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&scl=1&style=8&x={x}&y={y}&z={z}";
         OneStreet:http://a.tile.openstreetmap.org/{z}/{x}/{y}.png

         h skeleton map light  http://mt2.google.cn/vt/lyrs=h&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         m 全地图   http://mt2.google.cn/vt/lyrs=m&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         p terrain+map  http://mt2.google.cn/vt/lyrs=p&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         r skeleton map dark   http://mt2.google.cn/vt/lyrs=r&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         y hybrid satellite map   http://mt1.google.cn/vt/lyrs=y&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         t 地形图   http://mt0.google.cn/vt/lyrs=t&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         s 卫星地图   http://mt3.google.cn/vt/lyrs=s&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         也可以进行组合，例如：s,r 或者 t,h   http://mt3.google.cn/vt/lyrs=t,h&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}
         */

        public string Tile_DownloadUserAgent { get; set; } = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";
        public string Tile_DownloadFolder { get; set; } = Parameters.TileDownloadPath;
        public bool Tile_CoverFile { get; set; } = false;
        public string Tile_FormatExtension { get; set; } = "png";
        public (int width, int height) Tile_TileSize { get; set; } = (256, 256);

        public ImageFormat Tile_ImageFormat
        {
            get
            {
                switch (Tile_FormatExtension.ToLower())
                {
                    case "jpg":
                        return ImageFormat.Jpeg;

                    case "png":
                        return ImageFormat.Png;

                    case "tiff":
                        return ImageFormat.Tiff;

                    case "bmp":
                        return ImageFormat.Bmp;

                    default:
                        throw new Exception("无法识别后缀名" + Tile_FormatExtension);
                }
            }
        }

        public DownloadInfo Tile_LastDownload { get; set; }

        private int requestTimeOut = 1000;
        public int Tile_ServerPort { get; set; } = 8080;
        public string Tile_ServerFormat { get; set; } = @"..\Download\{z}/{x}-{y}.{ext}";

        public int Tile_RequestTimeOut
        {
            get => requestTimeOut;
            set
            {
                if (value > 0)
                {
                    requestTimeOut = value;
                }
                this.Notify();
            }
        }

        private int readTimeOut = 1000;

        public int Tile_ReadTimeOut
        {
            get => readTimeOut;
            set
            {
                if (value > 0)
                {
                    readTimeOut = value;
                }
                this.Notify();
            }
        }

        public BrowseInfo Tile_BrowseInfo { get; set; } = new BrowseInfo();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}