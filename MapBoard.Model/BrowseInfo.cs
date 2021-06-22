using FzLib.Extension;
using System.ComponentModel;

namespace MapBoard.Model
{
    public class BrowseInfo : INotifyPropertyChanged
    {
        private double zoom = 200;

        public double Zoom
        {
            get => zoom;
            set
            {
                zoom = value;
                this.Notify(nameof(Zoom));
            }
        }

        private double sensitivity = 5;

        public double Sensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = value;
                this.Notify(nameof(Sensitivity));
            }
        }

        private double fps = 20;

        public double FPS
        {
            get => fps;
            set
            {
                fps = value;
                this.Notify(nameof(FPS));
            }
        }

        private double speed = 16;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Speed
        {
            get => speed;
            set
            {
                speed = value;
                this.Notify(nameof(Speed));
            }
        }

        private int recordInterval = 1000;

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
                this.Notify(nameof(RecordInterval));
            }
        }
        
        private int extraRecordDelay = 100;

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
                this.Notify(nameof(ExtraRecordDelay));
            }
        }

        private int angle = 60;

        public int Angle
        {
            get => angle;
            set
            {
                angle = value;
                this.Notify(nameof(Angle));
            }
        }
    }
}