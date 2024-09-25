using FzLib;
using System;
using System.ComponentModel;
using System.Drawing;

namespace MapBoard.Model
{
    /// <summary>
    /// 标注信息
    /// </summary>
    public class LabelInfo : INotifyPropertyChanged, ICloneable
    {
        private double minScale = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 允许重叠
        /// </summary>
        public bool AllowOverlap { get; set; }

        /// <summary>
        /// 允许重复
        /// </summary>
        public bool AllowRepeat { get; set; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Transparent;

        /// <summary>
        /// 是否加粗
        /// </summary>
        public bool Bold { get; set; }

        /// <summary>
        /// 碰撞策略（LabelDeconflictionStrategy）
        /// </summary>
        public int DeconflictionStrategy { get; set; } = 1;

        /// <summary>
        /// 表达式
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// 字体颜色
        /// </summary>
        public Color FontColor { get; set; } = Color.Black;

        /// <summary>
        /// 字体类型
        /// </summary>
        public string FontFamily { get; set; }

        /// <summary>
        /// 字体大小
        /// </summary>
        public double FontSize { get; set; } = 10;

        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color HaloColor { get; set; } = Color.FromArgb(255, 248, 220);

        /// <summary>
        /// 边框粗细
        /// </summary>
        public double HaloWidth { get; set; } = 1.5;

        /// <summary>
        /// 是否斜体
        /// </summary>
        public bool Italic { get; set; }

        /// <summary>
        /// 布局类型（LabelTextLayout）
        /// </summary>
        public int Layout { get; set; } = 0;

        /// <summary>
        /// 最小缩放等级
        /// </summary>
        public double MinScale
        {
            get => minScale;
            set => minScale = value >= 0 ? value : 0;
        }

        /// <summary>
        /// 外边框颜色
        /// </summary>
        public Color OutlineColor { get; set; } = Color.Transparent;

        /// <summary>
        /// 外边框粗细
        /// </summary>
        public double OutlineWidth { get; set; } = 0;

        /// <summary>
        /// 重复距离
        /// </summary>
        public int RepeatDistance { get; set; } = 100;

        /// <summary>
        /// 筛选表达式
        /// </summary>
        public string WhereClause { get; set; } = "";

        /// <summary>
        /// LabelDefinition.ToJson/FromJson的原始Json字符串。该值不为空时，使用该属性作为标签信息，其他属性全部失效。
        /// </summary>
        public string RawJson { get; set; }

        public object Clone()
        {
            return MemberwiseClone() as LabelInfo;
        }
    }
}