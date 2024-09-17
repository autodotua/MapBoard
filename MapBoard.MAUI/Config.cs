using FzLib;
using FzLib.DataStorage.Serialization;
using MapBoard.IO;
using MapBoard.Model;
using MapBoard.Models;
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
        private static readonly string path = FolderPaths.ConfigPath;
        private static Config instance;
        private bool canRotate = false;
        private bool enableBasemapCache = true;
        private bool isTracking;
        private string lastCrashFile;
        private bool groupLayers;
        private double maxScale = 100;
        private bool screenAlwaysOn = false;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 配置类单例
        /// </summary>
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
                    }
                }
                return instance;
            }
        }

        public bool AutoQuit { get; set; } = false;
        public List<BaseLayerInfo> BaseLayers { get; set; } = new List<BaseLayerInfo>();
        public bool CanRotate
        {
            get => canRotate;
            set => this.SetValueAndNotify(ref canRotate, value, nameof(CanRotate));
        }

        public bool EnableBasemapCache
        {
            get => enableBasemapCache;
            set => this.SetValueAndNotify(ref enableBasemapCache, value, nameof(EnableBasemapCache));
        }

        public GeoShareConfig GeoShare { get; set; } = new GeoShareConfig();
        public bool IsTracking
        {
            get => isTracking;
            set => this.SetValueAndNotify(ref isTracking, value, nameof(IsTracking));
        }

        public string LastCrashFile
        {
            get => lastCrashFile;
            set => this.SetValueAndNotify(ref lastCrashFile, value, nameof(LastCrashFile));
        }

        public bool GroupLayers
        {
            get => groupLayers;
            set => this.SetValueAndNotify(ref groupLayers, value, nameof(GroupLayers));
        }

        public double MaxScale
        {
            get => maxScale;
            set => this.SetValueAndNotify(ref maxScale, value, nameof(MaxScale));
        }

        public bool ScreenAlwaysOn
        {
            get => screenAlwaysOn;
            set => this.SetValueAndNotify(ref screenAlwaysOn, value, nameof(ScreenAlwaysOn));
        }

        public int TrackMinDistance { get; set; } = 2;

        public int TrackMinTimeSpan { get; set; } = 2;

        public int TrackNotificationUpdateTimeSpan { get; set; } = 10;

        public bool UseReticleInDraw {  get; set; }
        public bool UseReticleInMeasure {  get; set; }
        public int MeterSpeedAlgorithm { get; set; }
        public int MeterStayTooLongSecond { get; set; } = 5;

        /// <summary>
        /// 保存配置到默认文件
        /// </summary>
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