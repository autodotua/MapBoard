using FzLib;
using MapBoard.IO;
using MapBoard.Models;
using MapBoard.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class TrackViewViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<SimpleFile> gpxFiles = new ObservableCollection<SimpleFile>();
        private string status;

        private TrackService trackService;

        public TrackViewViewModel()
        {
            TrackService.CurrentChanged += TrackService_StaticPropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SimpleFile> GpxFiles
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

        public async Task LoadGpxFilesAsync()
        {
            List<SimpleFile> files = new List<SimpleFile>();
            await Task.Run(() =>
            {
                foreach (var file in Directory
                    .EnumerateFiles(FolderPaths.TrackPath, "*.gpx")
                    .OrderDescending()
                    .Take(50))
                {
                    files.Add(new SimpleFile(file));
                }
            });
            GpxFiles = new ObservableCollection<SimpleFile>(files);
        }
        private void TrackService_StaticPropertyChanged(object sender, EventArgs e)
        {
            TrackService = TrackService.Current;
        }
    }
}
