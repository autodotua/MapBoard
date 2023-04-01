using FzLib;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace MapBoard.Model
{
    /// <summary>
    /// 符号信息
    /// </summary>
    public class SymbolInfo : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 默认折线符号
        /// </summary>
        public static SymbolInfo DefaultLineSymbol =>
            new SymbolInfo()
            {
                OutlineWidth = 2,
                LineColor = ColorTranslator.FromHtml("#BB12EDED"),
            };

        /// <summary>
        /// 默认点符号
        /// </summary>
        public static SymbolInfo DefaultPointSymbol =>
            new SymbolInfo()
            {
                Size = 6,
                OutlineWidth = 1,
                FillColor = ColorTranslator.FromHtml("#FFE36C09"),
                LineColor = ColorTranslator.FromHtml("#FFF2F2F2"),
            };
        /// <summary>
        /// 默认多边形符号
        /// </summary>
        public static SymbolInfo DefaultPolygonSymbol =>
            new SymbolInfo()
            {
                OutlineWidth = 2,
                FillColor = ColorTranslator.FromHtml("#66008000"),
                LineColor = ColorTranslator.FromHtml("#FF92CDDC"),
            };

        /// <summary>
        /// 箭头位置（SimpleLineSymbolMarkerPlacement）
        /// </summary>
        public int Arrow { get; set; } = 0;

        /// <summary>
        /// 填充（点、多边形）颜色
        /// </summary>
        public Color FillColor { get; set; } = Color.Green;

        /// <summary>
        /// 填充风格（SimpleFillSymbolStyle）
        /// </summary>
        public int FillStyle { get; set; } = 6;

        /// <summary>
        /// 外描边（点、多边形）、线（折线）粗细
        /// </summary>
        public Color LineColor { get; set; } = Color.Red;

        /// <summary>
        /// 线风格（SimpleLineSymbolStyle）
        /// </summary>
        public int LineStyle { get; set; } = 5;

        /// <summary>
        /// 外描边（点、多边形）、线（折线）颜色
        /// </summary>
        public double OutlineWidth { get; set; } = 2;

        /// <summary>
        /// 点风格（SimpleMarketSymbolStyle）
        /// </summary>
        public int PointStyle { get; set; } = 0;

        /// <summary>
        /// 直径（点）
        /// </summary>
        public double Size { get; set; } = 6;
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}