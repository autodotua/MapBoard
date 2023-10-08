using FzLib;
using MapBoard.Services;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class TrackPageViewModel : INotifyPropertyChanged
    {
        private string status;

        private TrackService trackService;

        public TrackPageViewModel()
        {
            TrackService.StaticPropertyChanged += TrackService_StaticPropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string Status
        {
            get => status;
            set => this.SetValueAndNotify(ref status, value, nameof(Status));
        }

        public TrackService TrackService
        {
            get => trackService;
            set => this.SetValueAndNotify(ref trackService, value, nameof(TrackService));
        }

        private void TrackService_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TrackService.Current))
            {
                TrackService = TrackService.Current;
            }
        }

    }
}
