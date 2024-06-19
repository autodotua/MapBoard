using FzLib;
using MapBoard.IO.Gpx;
using MapBoard.Models;
using System.ComponentModel;

namespace MapBoard.Models
{
    public class GpxAndFileInfo : INotifyPropertyChanged
    {
        private Gpx gpx;

        public GpxAndFileInfo(string file)
        {
            File = new SimpleFile(file);
            Gpx = new Gpx()
            {
                Time = File.Time,
            };
        }
        private GpxAndFileInfo()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleFile File { get; private set; }

        public Gpx Gpx
        {
            get => gpx;
            set => this.SetValueAndNotify(ref gpx, value, nameof(Gpx));
        }

        public async Task LoadGpxAsync()
        {
            Gpx = await GpxSerializer.LoadMetadatasFromFileAsync(File.FullName);
        }
    }
}
