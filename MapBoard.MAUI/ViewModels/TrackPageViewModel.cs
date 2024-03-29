﻿using FzLib;
using MapBoard.IO;
using MapBoard.IO.Gpx;
using MapBoard.Models;
using MapBoard.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class TrackViewViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<GpxAndFileInfo> gpxFiles = new ObservableCollection<GpxAndFileInfo>();
        private string status;

        private TrackService trackService;

        public TrackViewViewModel()
        {
            TrackService.CurrentChanged += TrackService_StaticPropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<GpxAndFileInfo> GpxFiles
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
            List<GpxAndFileInfo> files = new List<GpxAndFileInfo>();
            await Task.Run(() =>
            {
                foreach (var file in Directory
                    .EnumerateFiles(FolderPaths.TrackPath, "*.gpx")
                    .OrderDescending()
                    .Take(50))
                {
                    files.Add(new GpxAndFileInfo(file)) ;
                }
            });
            GpxFiles = new ObservableCollection<GpxAndFileInfo>(files);
        }
        private void TrackService_StaticPropertyChanged(object sender, EventArgs e)
        {
            TrackService = TrackService.Current;
        }
    }
}
