using FzLib;
using MapBoard.IO;
using MapBoard.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class TrackPageViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<FileInfo> gpxFiles = new ObservableCollection<FileInfo>();
        private string status;

        private TrackService trackService;

        public TrackPageViewModel()
        {
            TrackService.StaticPropertyChanged += TrackService_StaticPropertyChanged;
            foreach (var file in Directory
                .EnumerateFiles(FolderPaths.TrackPath, "*.gpx")
                .OrderDescending()
                .Take(50))
            {
                GpxFiles.Add(new FileInfo(file));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FileInfo> GpxFiles
        {
            get => gpxFiles;
            set => this.SetValueAndNotify(ref gpxFiles, value, nameof(GpxFiles));
        }

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
