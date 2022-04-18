using FzLib;
using FzLib.DataStorage.Serialization;
using MapBoard.IO;
using MapBoard.Model;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;

namespace MapBoard
{
    public class Config : IJsonSerializable, INotifyPropertyChanged
    {
        public static readonly int WatermarkHeight = 72;
        private static Config instance;
        private static readonly string path = FolderPaths.ConfigPath;

        private int httpTimeOut = 1000;

        private int serverLayerLoadTimeout = 5000;

        private int theme = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler ThemeChanged;

        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Config();
                    try
                    {
                        instance.TryLoadFromJsonFile(path);
                    }
                    catch (Exception ex)
                    {
                        instance.LoadError = ex;
                    }
                }
                return instance;
            }
        }

        public bool AutoCatchToNearestVertex { get; set; } = true;

        public bool BackupWhenExit { get; set; } = false;

        public bool BackupWhenReplace { get; set; } = true;

        public List<double> BufferDistances { get; set; } = new List<double>();

        public List<BaseLayerInfo> BaseLayers { get; set; } = new List<BaseLayerInfo>();

        public CoordinateSystem BasemapCoordinateSystem { get; set; } = CoordinateSystem.WGS84;

        public int CatchDistance { get; set; } = 12;

        public bool CopyShpFileWhenExport { get; set; } = true;

        public bool Gpx_AutoSmooth { get; set; } = false;

        public int Gpx_AutoSmoothLevel { get; set; } = 5;

        public bool Gpx_AutoSmoothOnlyZ { get; set; } = false;

        public bool Gpx_DrawPoints { get; set; } = false;

        public bool Gpx_Height { get; set; } = false;

        public int Gpx_HeightExaggeratedMagnification { get; set; } = 5;

        public bool Gpx_RelativeHeight { get; set; } = false;

        public bool HideWatermark { get; set; } = true;

        public string LastFTP { get; set; } = null;

        public int LastLayerListGroupType { get; set; } = 0;

        public Exception LoadError { get; private set; }

        public int MaxBackupCount { get; set; } = 100;

        public double MaxScale { get; set; } = 100;

        public bool RemainAttribute { get; set; } = false;

        public int ServerLayerLoadTimeout
        {
            get => serverLayerLoadTimeout;
            set
            {
                Debug.Assert(value >= 100);
                serverLayerLoadTimeout = value;
                Parameters.LoadTimeout = TimeSpan.FromMilliseconds(value);
            }
        }

        public bool ShowLocation { get; set; } = false;

        public bool ShowNearestPointSymbol { get; set; } = true;

        public bool ShowSideBaseLayers { get; set; } = true;

        public bool ShowSideCompass { get; set; } = true;

        public bool ShowSideLocation { get; set; } = true;

        public bool ShowSideScaleBar { get; set; } = true;

        public bool ShowSideScaleButton { get; set; } = false;

        public bool ShowSideSearch { get; set; } = true;

        public bool SmoothScroll { get; set; } = true;

        public bool TapToSelect { get; set; } = false;

        public bool TapToSelectAllLayers { get; set; } = true;

        public int Theme
        {
            get => theme;
            set
            {
                theme = value;
                ThemeChanged?.Invoke(this, new EventArgs());
            }
        }

        public BrowseInfo Tile_BrowseInfo { get; set; } = new BrowseInfo();

        public bool Tile_CoverFile { get; set; } = false;

        public string Tile_DownloadFolder { get; set; } = FolderPaths.TileDownloadPath;

        public string Tile_DownloadUserAgent { get; set; } = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";

        public string Tile_FormatExtension { get; set; } = "png";

        public ImageFormat Tile_ImageFormat
        {
            get
            {
                return Tile_FormatExtension.ToLower() switch
                {
                    "jpg" => ImageFormat.Jpeg,
                    "png" => ImageFormat.Png,
                    "tiff" => ImageFormat.Tiff,
                    "bmp" => ImageFormat.Bmp,
                    _ => throw new Exception("无法识别后缀名" + Tile_FormatExtension),
                };
            }
        }

        public DownloadInfo Tile_LastDownload { get; set; } = null;

        public int HttpTimeOut
        {
            get => httpTimeOut;
            set
            {
                if (value > 0)
                {
                    httpTimeOut = value;
                }
            }
        }

        public string Tile_ServerFilePathFormat { get; set; } = @"{Download}/{z}/{x}-{y}.{ext}";

        public int Tile_ServerPort { get; set; } = 8080;

        public (int width, int height) Tile_TileSize { get; set; } = (256, 256);

        public TileSourceCollection Tile_Urls { get; set; } = new TileSourceCollection();

        public string HttpUserAgent { get; set; } = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";

        public bool UseCompactLayerList { get; set; } = false;

        public void Save()
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            this.Save(path, new JsonSerializerSettings().SetIndented());
        }
    }
}