using FzLib;
using System;
using System.ComponentModel;
using System.Drawing;

namespace MapBoard.Model
{
    public class LabelInfo : INotifyPropertyChanged, ICloneable
    {
        private bool @class;

        /// <summary>
        /// 是否允许重叠
        /// </summary>
        private bool allowOverlap;

        /// <summary>
        /// 是否允许重复
        /// </summary>
        private bool allowRepeat;

        private Color backgroundColor = Color.Transparent;
        private bool bold;
        private string customLabelExpression;
        private bool date;
        private Color fontColor = Color.Black;
        private string fontFamily;
        private double fontSize = 12;
        private Color haloColor = Color.FromArgb(255, 248, 220);
        private double haloWidth = 3;
        private bool info = true;
        private bool italic;

        /// <summary>
        /// 标签布局
        /// </summary>
        private int layout = 0;

        private double minScale = 0;
        private bool newLine;
        private Color outlineColor = Color.Transparent;
        private double outlineWidth = 0;

        private string whereClause = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AllowOverlap
        {
            get => allowOverlap;
            set => this.SetValueAndNotify(ref allowOverlap, value, nameof(AllowOverlap));
        }

        public bool AllowRepeat
        {
            get => allowRepeat;
            set => this.SetValueAndNotify(ref allowRepeat, value, nameof(AllowRepeat));
        }

        public Color BackgroundColor
        {
            get => backgroundColor;
            set => this.SetValueAndNotify(ref backgroundColor, value, nameof(BackgroundColor));
        }

        public bool Bold
        {
            get => bold;
            set => this.SetValueAndNotify(ref bold, value, nameof(Bold));
        }

        public bool Class
        {
            get => @class;
            set => this.SetValueAndNotify(ref @class, value, nameof(Class));
        }

        public string CustomLabelExpression
        {
            get => customLabelExpression;
            set => this.SetValueAndNotify(ref customLabelExpression, value, nameof(CustomLabelExpression));
        }

        public bool Date
        {
            get => date;
            set => this.SetValueAndNotify(ref date, value, nameof(Date));
        }

        public Color FontColor
        {
            get => fontColor;
            set => this.SetValueAndNotify(ref fontColor, value, nameof(FontColor));
        }

        public string FontFamily
        {
            get => fontFamily;
            set => this.SetValueAndNotify(ref fontFamily, value, nameof(FontFamily));
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

        public bool Info
        {
            get => info;
            set => this.SetValueAndNotify(ref info, value, nameof(Info));
        }

        public bool Italic
        {
            get => italic;
            set => this.SetValueAndNotify(ref italic, value, nameof(Italic));
        }

        public int Layout
        {
            get => layout;
            set => this.SetValueAndNotify(ref layout, value, nameof(Layout));
        }

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

        public string WhereClause
        {
            get => whereClause;
            set => this.SetValueAndNotify(ref whereClause, value, nameof(WhereClause));
        }

        public object Clone()
        {
            return MemberwiseClone() as LabelInfo;
        }
    }
}