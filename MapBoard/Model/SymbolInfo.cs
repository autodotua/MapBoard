using FzLib.Extension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Model
{
    public class SymbolInfo : INotifyPropertyChanged
    {
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
    }
}