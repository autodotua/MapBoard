using FzLib;
using MapBoard.IO;
using MapBoard.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class TrackViewViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<FileInfo> gpxFiles = new ObservableCollection<FileInfo>();
        private string status;

        private TrackService trackService;

        public TrackViewViewModel()
        {
            TrackService.StaticPropertyChanged += TrackService_StaticPropertyChanged;
            List<FileInfo> files = new List<FileInfo>();
            Task.Run(() =>
           {
               foreach (var file in Directory
                   .EnumerateFiles(FolderPaths.TrackPath, "*.gpx")
                   .OrderDescending()
                   .Take(50))
               {
                   var fileInfo = new FileInfo(file);
                   files.Add(fileInfo);
               }
           }).ContinueWith(a =>
           {
               GpxFiles = new ObservableCollection<FileInfo>(files);
           });

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
