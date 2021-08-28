using FzLib;
using FzLib.DataStorage.Serialization;
using MapBoard.Model;
using Newtonsoft.Json;
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
        private static string path = Parameters.ConfigPath;
        public static int WatermarkHeight = 72;
        private static Config instance;

        private bool backupWhenExit = true;

        private bool backupWhenReplace = true;

        private List<BaseLayerInfo> baseLayers = new List<BaseLayerInfo>();

        private CoordinateSystem basemapCoordinateSystem = CoordinateSystem.WGS84;

        private bool copyShpFileWhenExport = true;

        private bool gpx_AutoSmooth = true;

        private int gpx_AutoSmoothLevel = 5;

        private bool gpx_AutoSmoothOnlyZ = false;

        private bool gpx_Height = false;

        private int gpx_HeightExaggeratedMagnification = 5;

        private bool gpx_RelativeHeight = false;

        private bool hideWatermark = true;

        private int maxBackupCount = 100;

        private double maxScale = 100;

        private int readTimeOut = 1000;

        private bool remainDate = false;

        private bool remainKey = false;

        private bool remainLabel = false;

        private int requestTimeOut = 1000;

        private int serverLayerLoadTimeout = 5000;
        private int theme = 0;

        private BrowseInfo tile_BrowseInfo = new BrowseInfo();

        private bool tile_CoverFile = false;

        private string tile_DownloadFolder = Parameters.TileDownloadPath;

        private string tile_DownloadUserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";

        private string tile_FormatExtension = "png";

        private DownloadInfo tile_LastDownload;

        private string tile_ServerFormat = @"..\Download\{z}/{x}-{y}.{ext}";

        private int tile_ServerPort = 8080;

        private TileSourceCollection tile_Urls = new TileSourceCollection();

        private string tile_UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";

        private string url = "http://mt3.google.cn/vt/lyrs=s&hl=zh-CN&gl=cn&x={x}&y={y}&z={z}";

        private bool useCompactLayerList;

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

        public Exception LoadError { get; private set; }

        public bool BackupWhenExit
        {
            get => backupWhenExit;
            set => this.SetValueAndNotify(ref backupWhenExit, value, nameof(BackupWhenExit));
        }

        public bool BackupWhenReplace
        {
            get => backupWhenReplace;
            set => this.SetValueAndNotify(ref backupWhenReplace, value, nameof(BackupWhenReplace));
        }

        public List<BaseLayerInfo> BaseLayers
        {
            get => baseLayers;
            set => this.SetValueAndNotify(ref baseLayers, value, nameof(BaseLayers));
        }

        public CoordinateSystem BasemapCoordinateSystem
        {
            get => basemapCoordinateSystem;
            set => this.SetValueAndNotify(ref basemapCoordinateSystem, value, nameof(BasemapCoordinateSystem));
        }

        public bool CopyShpFileWhenExport
        {
            get => copyShpFileWhenExport;
            set => this.SetValueAndNotify(ref copyShpFileWhenExport, value, nameof(CopyShpFileWhenExport));
        }

        private bool tapToSelect;

        public bool TapToSelect
        {
            get => tapToSelect;
            set => this.SetValueAndNotify(ref tapToSelect, value, nameof(TapToSelect));
        }

        private bool tapToSelectAllLayers = true;

        public bool TapToSelectAllLayers
        {
            get => tapToSelectAllLayers;
            set => this.SetValueAndNotify(ref tapToSelectAllLayers, value, nameof(TapToSelectAllLayers));
        }

        private bool showLocation;

        public bool ShowLocation
        {
            get => showLocation;
            set => this.SetValueAndNotify(ref showLocation, value, nameof(ShowLocation));
        }

        public bool Gpx_AutoSmooth
        {
            get => gpx_AutoSmooth;
            set => this.SetValueAndNotify(ref gpx_AutoSmooth, value, nameof(Gpx_AutoSmooth));
        }

        public int Gpx_AutoSmoothLevel
        {
            get => gpx_AutoSmoothLevel;
            set => this.SetValueAndNotify(ref gpx_AutoSmoothLevel, value, nameof(Gpx_AutoSmoothLevel));
        }

        public bool Gpx_AutoSmoothOnlyZ
        {
            get => gpx_AutoSmoothOnlyZ;
            set => this.SetValueAndNotify(ref gpx_AutoSmoothOnlyZ, value, nameof(Gpx_AutoSmoothOnlyZ));
        }

        public bool Gpx_Height
        {
            get => gpx_Height;
            set => this.SetValueAndNotify(ref gpx_Height, value, nameof(Gpx_Height));
        }

        public int Gpx_HeightExaggeratedMagnification
        {
            get => gpx_HeightExaggeratedMagnification;
            set => this.SetValueAndNotify(ref gpx_HeightExaggeratedMagnification, value, nameof(Gpx_HeightExaggeratedMagnification));
        }

        public bool Gpx_RelativeHeight
        {
            get => gpx_RelativeHeight;
            set => this.SetValueAndNotify(ref gpx_RelativeHeight, value, nameof(Gpx_RelativeHeight));
        }

        private bool gpx_DrawPoints;

        public bool Gpx_DrawPoints
        {
            get => gpx_DrawPoints;
            set => this.SetValueAndNotify(ref gpx_DrawPoints, value, nameof(Gpx_DrawPoints));
        }

        private int lastLayerListGroupType;

        public int LastLayerListGroupType
        {
            get => lastLayerListGroupType;
            set => this.SetValueAndNotify(ref lastLayerListGroupType, value, nameof(LastLayerListGroupType));
        }

        public bool HideWatermark
        {
            get => hideWatermark;
            set => this.SetValueAndNotify(ref hideWatermark, value, nameof(HideWatermark));
        }

        public int MaxBackupCount
        {
            get => maxBackupCount;
            set => this.SetValueAndNotify(ref maxBackupCount, value, nameof(MaxBackupCount));
        }

        public double MaxScale
        {
            get => maxScale;
            set => this.SetValueAndNotify(ref maxScale, value, nameof(MaxScale));
        }

        public bool RemainDate
        {
            get => remainDate;
            set => this.SetValueAndNotify(ref remainDate, value, nameof(RemainDate));
        }

        public bool RemainKey
        {
            get => remainKey;
            set => this.SetValueAndNotify(ref remainKey, value, nameof(RemainKey));
        }

        public bool RemainLabel
        {
            get => remainLabel;
            set => this.SetValueAndNotify(ref remainLabel, value, nameof(RemainLabel));
        }

        public int ServerLayerLoadTimeout
        {
            get => serverLayerLoadTimeout;
            set
            {
                Debug.Assert(value >= 100);
                this.SetValueAndNotify(ref serverLayerLoadTimeout, value, nameof(ServerLayerLoadTimeout));
                Parameters.LoadTimeout = TimeSpan.FromMilliseconds(value);
            }
        }

        public int Theme
        {
            get => theme;
            set
            {
                theme = value;
                ThemeChanged?.Invoke(this, new EventArgs());
            }
        }

        public BrowseInfo Tile_BrowseInfo
        {
            get => tile_BrowseInfo;
            set => this.SetValueAndNotify(ref tile_BrowseInfo, value, nameof(Tile_BrowseInfo));
        }

        public bool Tile_CoverFile
        {
            get => tile_CoverFile;
            set => this.SetValueAndNotify(ref tile_CoverFile, value, nameof(Tile_CoverFile));
        }

        public string Tile_DownloadFolder
        {
            get => tile_DownloadFolder;
            set => this.SetValueAndNotify(ref tile_DownloadFolder, value, nameof(Tile_DownloadFolder));
        }

        public string Tile_DownloadUserAgent
        {
            get => tile_DownloadUserAgent;
            set => this.SetValueAndNotify(ref tile_DownloadUserAgent, value, nameof(Tile_DownloadUserAgent));
        }

        public string Tile_FormatExtension
        {
            get => tile_FormatExtension;
            set => this.SetValueAndNotify(ref tile_FormatExtension, value, nameof(Tile_FormatExtension));
        }

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

        public DownloadInfo Tile_LastDownload
        {
            get => tile_LastDownload;
            set => this.SetValueAndNotify(ref tile_LastDownload, value, nameof(Tile_LastDownload));
        }

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

        public string Tile_ServerFormat
        {
            get => tile_ServerFormat;
            set => this.SetValueAndNotify(ref tile_ServerFormat, value, nameof(Tile_ServerFormat));
        }

        public int Tile_ServerPort
        {
            get => tile_ServerPort;
            set => this.SetValueAndNotify(ref tile_ServerPort, value, nameof(Tile_ServerPort));
        }

        public (int width, int height) Tile_TileSize { get; set; } = (256, 256);

        public TileSourceCollection Tile_Urls
        {
            get => tile_Urls;
            set => this.SetValueAndNotify(ref tile_Urls, value, nameof(Tile_Urls));
        }

        public string Tile_UserAgent
        {
            get => tile_UserAgent;
            set => this.SetValueAndNotify(ref tile_UserAgent, value, nameof(Tile_UserAgent));
        }

        public string Url
        {
            get => url;
            set => this.SetValueAndNotify(ref url, value, nameof(Url));
        }

        public bool UseCompactLayerList
        {
            get => useCompactLayerList;
            set => this.SetValueAndNotify(ref useCompactLayerList, value, nameof(UseCompactLayerList));
        }

        public void Save()
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            this.Save(path, new JsonSerializerSettings().SetIndented());
        }

        private bool smoothScroll;

        public bool SmoothScroll
        {
            get => smoothScroll;
            set => this.SetValueAndNotify(ref smoothScroll, value, nameof(SmoothScroll));
        }
    }
}