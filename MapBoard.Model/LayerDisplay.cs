using FzLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MapBoard.Model
{
    public class LayerDisplay : INotifyPropertyChanged
    {
        private double maxScale = 0;
        private double minScale = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public double MaxScale
        {
            get => maxScale;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
            }
        }

        public double MinScale
        {
            get => minScale;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
            }
        }

        public double Opacity { get; set; } = 1;
    }
}