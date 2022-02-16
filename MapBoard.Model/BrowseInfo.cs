using FzLib;
using System.ComponentModel;

namespace MapBoard.Model
{
    public class BrowseInfo : INotifyPropertyChanged
    {
        private int extraRecordDelay = 100;
        private int recordInterval = 1000;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Angle { get; set; } = 60;

        public int ExtraRecordDelay
        {
            get => extraRecordDelay;
            set
            {
                if (value < 1000)
                {
                    value = value / 10 * 10;
                }
                else if (value < 10000)
                {
                    value = value / 100 * 100;
                }
                extraRecordDelay = value;
            }
        }

        public double FPS { get; set; } = 20;

        public int RecordInterval
        {
            get => recordInterval;
            set
            {
                if (value < 1000)
                {
                    value = value / 10 * 10;
                }
                else if (value < 10000)
                {
                    value = value / 500 * 500;
                }
                else
                {
                    value = value / 1000 * 1000;
                }
                recordInterval = value;
            }
        }

        public double Sensitivity { get; set; } = 5;
        public double Speed { get; set; } = 16;
        public double Zoom { get; set; } = 200;
    }
}