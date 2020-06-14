using FzLib.Extension;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using IOPath = System.IO.Path;

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
                    instance = TryOpenOrCreate<Config>(System.IO.Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "config.json"));
                    instance.Settings.Formatting = Formatting.Indented;
                }
                return instance;
            }
        }

        public List<BaseLayerInfo> BaseLayers { get; } = new List<BaseLayerInfo>();
        public static string DataPath { get; } = @"..\Data";

        public bool StaticEnable { get; set; } = true;

        public double StaticWidth { get; set; } = 100;

        public bool GpxHeight { get; set; } = true;
        public double GpxHeightExaggeratedMagnification { get; set; } = 5;
        public bool GpxRelativeHeight { get; set; } = true;
        public BrowseInfo BrowseInfo { get; set; } = new BrowseInfo();
        public override void Save()
        {
            base.Save();
        }

        public string BasemapCoordinateSystem { get; set; } = "WGS84";

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

    }

    public class BaseLayerInfo : INotifyPropertyChanged, ICloneable
    {
        private int index;

        public BaseLayerInfo(BaseLayerType type, string path)
        {
            Type = type;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public BaseLayerType Type { get; set; }
        public string Path { get; set; }
        public int Index
        {
            get => index;
            set => this.SetValueAndNotify(ref index, value, nameof(Index));
        }
        private bool enable = true;
        public bool Enable
        {
            get => enable;
            set
            {
                enable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enable)));
            }
        }
        private double opacity = 1;
        public double Opacity
        {
            get => opacity;
            set
            {
                opacity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Opacity)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public BaseLayerInfo Clone()
        {
            return MemberwiseClone() as BaseLayerInfo;
        }

        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
    }

    public enum BaseLayerType
    {
        WebTiledLayer,
        RasterLayer,
        ShapefileLayer,
        TpkLayer
    }
}

