using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.TileDownloaderSplicer
{
    public class TileSourceInfo : FzLib.Extension.ExtendedINotifyPropertyChanged
    {
        public string Name { get => name; set => SetValueAndNotify(ref name, value, nameof(Name)); }
        private string url;
        private string name;

        public string Url
        {
            get => url;
            set => SetValueAndNotify(ref url, value, nameof(Url));
        }
    }
}
