using FzLib.Extension;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.TileDownloaderSplicer
{
    public class TileSourceInfo : INotifyPropertyChanged
    {
        public string Name { get => name; set => this.SetValueAndNotify(ref name, value, nameof(Name)); }
        private string url;
        private string name;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Url
        {
            get => url;
            set => this.SetValueAndNotify(ref url, value, nameof(Url));
        }
    }
}