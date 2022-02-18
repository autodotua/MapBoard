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
        private double opacity = 1;

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
                maxScale = value;
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
                minScale = value;
            }
        }

        public double Opacity
        {
            get => opacity;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > 1)
                {
                    value = 1;
                }
                opacity = value;
            }
        }
    }
}