using FzLib;
using MapBoard.IO;
using MapBoard.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class ImportViewVideModel : INotifyPropertyChanged
    {
        private ObservableCollection<SimpleFile> files = new ObservableCollection<SimpleFile>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SimpleFile> Files
        {
            get => files;
            set => this.SetValueAndNotify(ref files, value, nameof(Files));
        }

        public async Task LoadFilesAsync()
        {
            List<SimpleFile> files = new List<SimpleFile>();
            await Task.Run(() =>
            {
                foreach (var file in Directory
                    .EnumerateFiles(FolderPaths.PackagePath, "*.mbmpkg")
                    .OrderDescending()
                    .Take(50))
                {
                    files.Add(new SimpleFile(file));
                }
            });
            Files = new ObservableCollection<SimpleFile>(files);
        }
    }
}
