using FzLib;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace MapBoard.Model
{
    public class BaseLayerInfo : INotifyPropertyChanged, ICloneable
    {
        public BaseLayerInfo()
        {
        }

        public BaseLayerInfo(BaseLayerType type, string path)
        {
            Type = type;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 是否加载图层
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 用于在UI中显示的顺序号
        /// </summary>
        [JsonIgnore]
        public int Index { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// 不透明度
        /// </summary>
        public double Opacity { get; set; } = 1;

        /// <summary>
        /// 底图的路径，若是瓦片图则是Url，若是其它则是文件地址
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 用于识别ArcGIS中图层的ID
        /// </summary>
        [JsonIgnore]
        public Guid TempID { get; } = Guid.NewGuid();

        /// <summary>
        /// 底图的类型
        /// </summary>
        public BaseLayerType Type { get; set; }

        /// <summary>
        /// 图层名称
        /// </summary>
        /// <summary>
        /// 是否显示图层
        /// </summary>
        public bool Visible { get; set; } = true;

        //以下是一些网络参数，不包含UI，仅能在json配置中手动修改
        public string UserAgent { get; set; } 
        public string Host { get; set; } 
        public string Referer { get; set; } 
        public string Origin { get; set; } 
        public string OtherHeaders { get; set; }

        public BaseLayerInfo Clone()
        {
            return MemberwiseClone() as BaseLayerInfo;
        }

        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
    }
}