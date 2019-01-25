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
    public class TileUrlInfo : FzLib.Extension.ExtendedINotifyPropertyChanged
    {
        public string Name { get; set; }
        private string url;
        public string Url
        {
            get => url;
            set => SetValueAndNotify(ref url, value, nameof(Url));
        }
    }
}
