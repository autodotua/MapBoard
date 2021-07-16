using FzLib;
using System;
using System.ComponentModel;
using System.Drawing;

namespace MapBoard.Model
{
    public class SymbolInfo : INotifyPropertyChanged, ICloneable
    {
        public static SymbolInfo DefaultPointSymbol =>
            new SymbolInfo()
            {
                Size = 12,
                OutlineWidth = 2,
                FillColor = ColorTranslator.FromHtml("#FFE36C09"),
                LineColor = ColorTranslator.FromHtml("#FFF2F2F2"),
            };

        public static SymbolInfo DefaultLineSymbol =>
            new SymbolInfo()
            {
                OutlineWidth = 6,
                LineColor = ColorTranslator.FromHtml("#BB12EDED"),
            };

        public static SymbolInfo DefaultPolygonSymbol =>
            new SymbolInfo()
            {
                OutlineWidth = 4,
                FillColor = ColorTranslator.FromHtml("#66008000"),
                LineColor = ColorTranslator.FromHtml("#FF92CDDC"),
            };

        public SymbolInfo()
        {
        }

        private double size = 6;

        public double Size
        {
            get => size;
            set => this.SetValueAndNotify(ref size, value, nameof(Size));
        }

        private double outlineWidth = 6;

        public double OutlineWidth
        {
            get => outlineWidth;
            set => this.SetValueAndNotify(ref outlineWidth, value, nameof(OutlineWidth));
        }

        private Color lineColor = Color.Red;

        public Color LineColor
        {
            get => lineColor;
            set => this.SetValueAndNotify(ref lineColor, value, nameof(LineColor));
        }

        private Color fillColor = Color.Green;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color FillColor
        {
            get => fillColor;
            set => this.SetValueAndNotify(ref fillColor, value, nameof(FillColor));
        }

        private int pointStyle = 0;

        public int PointStyle
        {
            get => pointStyle;
            set => this.SetValueAndNotify(ref pointStyle, value, nameof(PointStyle));
        }

        private int fillStyle = 6;

        public int FillStyle
        {
            get => fillStyle;
            set => this.SetValueAndNotify(ref fillStyle, value, nameof(FillStyle));
        }

        private int lineStyle = 5;

        public int LineStyle
        {
            get => lineStyle;
            set => this.SetValueAndNotify(ref lineStyle, value, nameof(LineStyle));
        }

        private int arrow = 0;

        public int Arrow
        {
            get => arrow;
            set => this.SetValueAndNotify(ref arrow, value, nameof(Arrow));
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}