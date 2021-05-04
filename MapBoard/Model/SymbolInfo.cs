using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Model
{
    public class SymbolInfo
    {
        public double LineWidth { get; set; } = 5;

        public Color LineColor { get; set; } = Color.Red;
        public Color FillColor { get; set; } = Color.Green;
    }
}