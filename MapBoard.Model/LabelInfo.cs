using FzLib;
using System;
using System.ComponentModel;
using System.Drawing;

namespace MapBoard.Model
{
    public class LabelInfo : INotifyPropertyChanged, ICloneable
    {
        private double minScale = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AllowOverlap { get; set; }

        public bool AllowRepeat { get; set; }

        public Color BackgroundColor { get; set; } = Color.Transparent;

        public bool Bold { get; set; }

        public int DeconflictionStrategy { get; set; } = 1;
        public string Expression { get; set; }

        public Color FontColor { get; set; } = Color.Black;

        public string FontFamily { get; set; }

        public double FontSize { get; set; } = 10;

        public Color HaloColor { get; set; } = Color.FromArgb(255, 248, 220);

        public double HaloWidth { get; set; } = 1.5;

        public bool Italic { get; set; }

        public int Layout { get; set; } = 0;
        public double MinScale
        {
            get => minScale;
            set => minScale = value >= 0 ? value : 0;
        }

        public Color OutlineColor { get; set; } = Color.Transparent;
        public double OutlineWidth { get; set; } = 0;
        public int RepeatDistance { get; set; } = 100;
        public string WhereClause { get; set; } = "";

        public object Clone()
        {
            return MemberwiseClone() as LabelInfo;
        }
    }
}