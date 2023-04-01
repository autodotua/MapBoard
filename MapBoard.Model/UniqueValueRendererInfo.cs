using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace MapBoard.Model
{
    /// <summary>
    /// 唯一值渲染器信息
    /// </summary>
    public class UniqueValueRendererInfo : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 富豪的总数
        /// </summary>
        [JsonIgnore]
        public int Count => Symbols.Count;

        /// <summary>
        /// 默认符号
        /// </summary>
        public SymbolInfo DefaultSymbol { get; set; }

        /// <summary>
        /// 是否存在自定义符号。若不存在，说明只是简单渲染。
        /// </summary>
        [JsonIgnore]
        public bool HasCustomSymbols => !string.IsNullOrEmpty(KeyFieldName) && Symbols.Count > 0;

        /// <summary>
        /// 作为Key的字段名。多个字段名使用“|”分割。
        /// </summary>
        public string KeyFieldName { get; set; }

        /// <summary>
        /// 不同的符号，对应的值。多个值使用“|”分割，数量应与<see cref="KeyFieldName"/>相同。
        /// </summary>
        public Dictionary<string, SymbolInfo> Symbols { get; set; } = new Dictionary<string, SymbolInfo>();

        /// <summary>
        /// 建立副本
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var info = MemberwiseClone() as UniqueValueRendererInfo;
            info.Symbols = new Dictionary<string, SymbolInfo>(Symbols);
            foreach (var key in info.Symbols.Keys)
            {
                info.Symbols[key] = info.Symbols[key].Clone() as SymbolInfo;
            }
            return info;
        }
    }
}