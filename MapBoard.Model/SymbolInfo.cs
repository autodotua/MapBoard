using FzLib;
using System;
using System.ComponentModel;
using System.Diagnostics;
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

        public double Size { get; set; } = 6;

        public double OutlineWidth { get; set; } = 6;

        public Color LineColor { get; set; } = Color.Red;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color FillColor { get; set; } = Color.Green;

        public int PointStyle { get; set; } = 0;

        public int FillStyle { get; set; } = 6;

        public int LineStyle { get; set; } = 5;

        public int Arrow { get; set; } = 0;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}