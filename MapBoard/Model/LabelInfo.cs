using FzLib.Extension;
using System;
using System.ComponentModel;
using System.Drawing;
using static FzLib.Extension.ExtendedINotifyPropertyChanged;

namespace MapBoard.Main.Model
{
    public class LabelInfo : INotifyPropertyChanged, ICloneable
    {
        private Color backgroundColor = Color.Transparent;
        private Color fontColor = Color.Black;
        private double fontSize = 12;
        private Color haloColor = Color.FromArgb(255, 248, 220);
        private double haloWidth = 3;
        private double minScale = 0;
        private bool newLine;
        private Color outlineColor = Color.Transparent;
        private double outlineWidth = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 是否允许重叠
        /// </summary>
        public bool AllowOverlap { get; set; }

        /// <summary>
        /// 是否允许重复
        /// </summary>
        public bool AllowRepeat { get; set; }

        public Color BackgroundColor
        {
            get => backgroundColor;
            set => this.SetValueAndNotify(ref backgroundColor, value, nameof(BackgroundColor));
        }

        public bool Class { get; set; }
        public bool Date { get; set; }
        public bool Enable => Info || Date || Class;

        public Color FontColor
        {
            get => fontColor;
            set => this.SetValueAndNotify(ref fontColor, value, nameof(FontColor));
        }

        public double FontSize
        {
            get => fontSize;
            set => this.SetValueAndNotify(ref fontSize, value, nameof(FontSize));
        }

        public Color HaloColor
        {
            get => haloColor;
            set => this.SetValueAndNotify(ref haloColor, value, nameof(HaloColor));
        }

        public double HaloWidth
        {
            get => haloWidth;
            set => this.SetValueAndNotify(ref haloWidth, value, nameof(HaloWidth));
        }

        public bool Info { get; set; } = true;

        /// <summary>
        /// 标签布局
        /// </summary>
        public int Layout { get; set; } = 0;

        public double MinScale
        {
            get => minScale;
            set
            {
                if (value >= 0)
                {
                    minScale = value;
                }
                else
                {
                    minScale = 0;
                }
                this.Notify(nameof(MinScale));
            }
        }

        public bool NewLine
        {
            get => newLine;
            set => this.SetValueAndNotify(ref newLine, value, nameof(NewLine));
        }

        public Color OutlineColor
        {
            get => outlineColor;
            set => this.SetValueAndNotify(ref outlineColor, value, nameof(OutlineColor));
        }

        public double OutlineWidth
        {
            get => outlineWidth;
            set => this.SetValueAndNotify(ref outlineWidth, value, nameof(OutlineWidth));
        }

        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public string FontFamily { get; set; }

        public object Clone()
        {
            return MemberwiseClone() as LabelInfo;
        }
    }
}