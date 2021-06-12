using FzLib.Extension;
using MapBoard.Common.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Common
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

        private List<BaseLayerInfo> baseLayers = new List<BaseLayerInfo>();

        public List<BaseLayerInfo> BaseLayers
        {
            get => baseLayers;
            set => this.SetValueAndNotify(ref baseLayers, value, nameof(BaseLayers));
        }

        public bool GpxHeight { get; set; } = true;
        public double GpxHeightExaggeratedMagnification { get; set; } = 5;
        public bool GpxRelativeHeight { get; set; } = true;
        public BrowseInfo BrowseInfo { get; set; } = new BrowseInfo();

        public override void Save()
        {
            base.Save();
        }

        public string BasemapCoordinateSystem { get; set; } = "WGS84";
        public bool HideWatermark { get; set; } = true;
        public static int WatermarkHeight = 72;
        public bool RemainLabel { get; set; } = false;
        public bool RemainKey { get; set; } = false;
        public bool RemainDate { get; set; } = false;
        public bool BackupWhenExit { get; set; } = true;
        public bool BackupWhenReplace { get; set; } = true;
        public int MaxBackupCount { get; set; } = 100;

        public bool GpxAutoSmooth { get; set; } = true;
        public bool GpxAutoSmoothOnlyZ { get; set; } = false;
        public int GpxAutoSmoothLevel { get; set; } = 5;
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

        public int RequestTimeOut { get; set; } = 1000;
        public int ReadTimeOut { get; set; } = 1000;
        public string UserAgent { get; set; } = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; QQWubi 133; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; CIBA; InfoPath.2)";

        public event EventHandler ThemeChanged;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class BrowseInfo : INotifyPropertyChanged
    {
        private double zoom = 200;

        public double Zoom
        {
            get => zoom;
            set
            {
                zoom = value;
                this.Notify(nameof(Zoom));
            }
        }

        private double sensitivity = 5;

        public double Sensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = value;
                this.Notify(nameof(Sensitivity));
            }
        }

        private double fps = 20;

        public double FPS
        {
            get => fps;
            set
            {
                fps = value;
                this.Notify(nameof(FPS));
            }
        }

        private double speed = 16;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Speed
        {
            get => speed;
            set
            {
                speed = value;
                this.Notify(nameof(Speed));
            }
        }

        private int recordInterval = 1000;

        public int RecordInterval
        {
            get => recordInterval;
            set
            {
                if (value < 1000)
                {
                    value = value / 10 * 10;
                }
                else if (value < 10000)
                {
                    value = value / 500 * 500;
                }
                else
                {
                    value = value / 1000 * 1000;
                }
                recordInterval = value;
                this.Notify(nameof(RecordInterval));
            }
        }

        private int angle = 60;

        public int Angle
        {
            get => angle;
            set
            {
                angle = value;
                this.Notify(nameof(Angle));
            }
        }
    }
}