﻿using FzLib;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace MapBoard.Model
{
    /// <summary>
    /// 底图图层
    /// </summary>
    public class BaseLayerInfo : INotifyPropertyChanged, ICloneable
    {
        public BaseLayerInfo()
        {
        }

        public BaseLayerInfo(BaseLayerType type, string path)
        {
            Name = "未命名";
            Type = type;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 亮度值（-100~100）
        /// </summary>
        public double Brightness { get; set; } = 0;

        /// <summary>
        /// 拉伸颜色参数
        /// </summary>
        public string ColorRampParameters { get; set; }

        /// <summary>
        /// 对比度（-100~100）
        /// </summary>
        public double Contrast { get; set; } = 0;

        /// <summary>
        /// 是否加载图层
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 伽马曲线（-100~100）
        /// </summary>
        public double Gamma { get; set; }

        /// <summary>
        /// HTTP请求头Host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 用于在UI中显示的顺序号
        /// </summary>
        [JsonIgnore]
        public int Index { get; set; }

        /// <summary>
        /// 最大缩放比例
        /// </summary>
        public int MaxLevel { get; set; } = -1;

        /// <summary>
        /// 最小缩放比例
        /// </summary>
        public int MinLevel { get; set; } = -1;

        /// <summary>
        /// 图层名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 不透明度
        /// </summary>
        public double Opacity { get; set; } = 1;

        /// <summary>
        /// HTTP请求头Origin
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// HTTP请求头中其他属性（无用）
        /// </summary>
        public string OtherHeaders { get; set; }

        /// <summary>
        /// 底图的路径，若是瓦片图则是Url，若是其它则是文件地址
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// HTTP请求头Referer
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        /// 渲染器
        /// </summary>
        public string Renderer { get; set; }

        /// <summary>
        /// 拉伸参数
        /// </summary>
        public string StretchParameters { get; set; }
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
        /// HTTP请求头User-Agent
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// 是否显示图层
        /// </summary>
        public bool Visible { get; set; } = true;
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