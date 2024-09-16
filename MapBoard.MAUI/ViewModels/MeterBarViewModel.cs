using FzLib;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class MeterBarViewModel : INotifyPropertyChanged
    {
        private double distance;
        private double speed;

        private DateTime time;

        public MeterBarViewModel()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public double Distance
        {
            get => distance;
            set => this.SetValueAndNotify(ref distance, value, nameof(Distance));
        }

        public double Speed
        {
            get => speed;
            set => this.SetValueAndNotify(ref speed, value, nameof(Speed));
        }
        public DateTime Time
        {
            get => time;
            set => this.SetValueAndNotify(ref time, value, nameof(Time));
        }
    }
}
