using FzLib.Extension;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace MapBoard.Model
{
    public class BaseLayerInfo : INotifyPropertyChanged, ICloneable
    {
        private int index;

        public BaseLayerInfo()
        {
        }

        public BaseLayerInfo(BaseLayerType type, string path)
        {
            Type = type;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>
        /// 底图的类型
        /// </summary>
        public BaseLayerType Type { get; set; }

        /// <summary>
        /// 底图的路径，若是瓦片图则是Url，若是其它则是文件地址
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 图层名称
        /// </summary>

        public string Name { get; set; }

        /// <summary>
        /// 用于在UI中显示的顺序号
        /// </summary>
        [JsonIgnore]
        public int Index
        {
            get => index;
            set => this.SetValueAndNotify(ref index, value, nameof(Index));
        }

        private bool enable = true;

        /// <summary>
        /// 是否显示图层
        /// </summary>
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

        /// <summary>
        /// 不透明度
        /// </summary>
        public double Opacity
        {
            get => opacity;
            set
            {
                opacity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Opacity)));
            }
        }

        /// <summary>
        /// 用于识别ArcGIS中图层的ID
        /// </summary>
        [JsonIgnore]
        public Guid TempID { get; } = Guid.NewGuid();

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