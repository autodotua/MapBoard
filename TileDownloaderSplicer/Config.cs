using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.TileDownloaderSplicer
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
                    instance = TryOpenOrCreate<Config>("MapBoard.TileDownloaderSplicerConfig.json");
                    if (instance.UrlCollection.Sources.Count == 0)
                    {
                        instance.UrlCollection.Sources.Add(new TileSourceInfo() { Name = "高德地图", Url = "http://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&scl=1&style=8&x={x}&y={y}&z={z}" });
                        instance.UrlCollection.Sources.Add(new TileSourceInfo() { Name = "谷歌卫星", Url = "http://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={z}" });
                        instance.UrlCollection.Sources.Add(new TileSourceInfo() { Name = "谷歌卫星中国（GCJ02）", Url = "http://mt1.google.cn/vt/lyrs=s&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}" });
                        instance.UrlCollection.Sources.Add(new TileSourceInfo() { Name = "谷歌卫星中国（WGS84）", Url = "http://mt1.google.cn/vt/lyrs=s&x={x}&y={y}&z={z}" });
                        instance.UrlCollection.Sources.Add(new TileSourceInfo() { Name = "天地图", Url = "http://t0.tianditu.com/vec_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=vec&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=9396357d4b92e8e197eafa646c3c541d" });
                        instance.UrlCollection.Sources.Add(new TileSourceInfo() { Name = "天地图注记", Url = "http://t0.tianditu.com/cva_w/wmts?service=WMTS&request=GetTile&version=1.0.0&layer=cva&style=default&TILEMATRIXSET=w&format=tiles&height=256&width=256&tilematrix={z}&tilerow={y}&tilecol={x}&tk=9396357d4b92e8e197eafa646c3c541d" });

                    }
                }
                return instance;
            }
        }

        public Config()
        {
            Settings.Formatting = Newtonsoft.Json.Formatting.Indented;
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
        public string DownloadFolder { get; set; } = "Download";
        public bool CoverFile { get; set; } = true;
        public string FormatExtension { get; set; } = "jpg";
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
        public int ServerPort = 8080;
        public int RequestTimeOut
        {
            get => requestTimeOut;
            set
            {
                if(value>0)
                {
                    requestTimeOut = value;
                }
                Notify(nameof(RequestTimeOut));
            }
        }
        private int readTimeOut = 1000;
        public int ReadTimeOut
        {
            get => readTimeOut;
            set
            {
                if (value > 0)
                {
                    readTimeOut = value;
                }
                Notify(nameof(ReadTimeOut));
            }
        }
        public override void Save()
        {
            base.Save();
        }
    }
}
