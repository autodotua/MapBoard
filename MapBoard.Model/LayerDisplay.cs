using FzLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MapBoard.Model
{
    public class LayerDisplay : INotifyPropertyChanged
    {
        private double minScale = 0;

        public double MinScale
        {
            get => minScale;
            set
            {
                if(value<0)
                {
                    value = 0;
                }
                this.SetValueAndNotify(ref minScale, value, nameof(MinScale));
            }
        }

        private double maxScale = 0;

        public double MaxScale
        {
            get => maxScale;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                this.SetValueAndNotify(ref maxScale, value, nameof(MaxScale));
            }
        }

        private double opacity = 1;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Opacity
        {
            get => opacity;
            set => this.SetValueAndNotify(ref opacity, value, nameof(Opacity));
        }
    }
}