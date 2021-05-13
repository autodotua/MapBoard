using FzLib.Extension;
using MapBoard.TileDownloaderSplicer.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.TileDownloaderSplicer
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
                    instance = TryOpenOrCreate<Config>(System.IO.Path.Combine(System.IO.Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "config_tile.json")));
                    instance.Settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
                }
                return instance;
            }
        }

        public Config()
        {
        }

        public TileSourceCollection UrlCollection { get; set; } = new TileSourceCollection();
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

        public string DownloadUserAgent { get; set; } = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";
        public string DownloadFolder { get; set; } = @"..\Download";
        public bool CoverFile { get; set; } = false;
        public string FormatExtension { get; set; } = "png";
        public (int width, int height) TileSize { get; set; } = (256, 256);

        public ImageFormat ImageFormat
        {
            get
            {
                switch (FormatExtension.ToLower())
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
                        throw new Exception("无法识别后缀名" + FormatExtension);
                }
            }
        }

        public DownloadInfo LastDownload { get; set; }

        private int requestTimeOut = 1000;
        public int ServerPort { get; set; } = 8080;
        public string ServerFormat { get; set; } = @"..\Download\{z}/{x}-{y}.{ext}";

        public int RequestTimeOut
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

        public event PropertyChangedEventHandler PropertyChanged;

        public int ReadTimeOut
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

        public override void Save()
        {
            base.Save();
        }
    }
}