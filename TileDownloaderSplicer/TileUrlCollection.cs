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
    public class TileUrlCollection : FzLib.Extension.ExtendedINotifyPropertyChanged
    {
        public TileUrlCollection()
        {
            Urls.CollectionChanged += (p1, p2) =>
              {
                  if (p2.NewItems != null)
                  {
                      (p2.NewItems[0] as TileUrlInfo).PropertyChanged += (p3, p4) =>
                      {
                          if (p3 == SelectedUrl)
                          {
                              Notify(nameof(SelectedUrl));
                          }
                      };
                  }
              };
        }
        public ObservableCollection<TileUrlInfo> Urls { get; set; } = new ObservableCollection<TileUrlInfo>();
        private int selectedIndex;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                SetValueAndNotify(ref selectedIndex, value, nameof(SelectedUrl));
            }
        }

        public TileUrlInfo SelectedUrl => (SelectedIndex == -1 || SelectedIndex >= Urls.Count) ? null : Urls[SelectedIndex];

    }
}
