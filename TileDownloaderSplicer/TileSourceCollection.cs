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
    public class TileSourceCollection : FzLib.Extension.ExtendedINotifyPropertyChanged
    {
        public TileSourceCollection()
        {
            Sources.CollectionChanged += (p1, p2) =>
              {
                  if (p2.NewItems != null)
                  {
                      (p2.NewItems[0] as TileSourceInfo).PropertyChanged += (p3, p4) =>
                      {
                          if (p3 == SelectedUrl)
                          {
                              Notify(nameof(SelectedUrl));
                          }
                      };
                  }
              };
        }
        public ObservableCollection<TileSourceInfo> Sources { get; set; } = new ObservableCollection<TileSourceInfo>();
        private int selectedIndex;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                SetValueAndNotify(ref selectedIndex, value, nameof(SelectedUrl));
            }
        }

        public TileSourceInfo SelectedUrl => (SelectedIndex == -1 || SelectedIndex >= Sources.Count) ? null : Sources[SelectedIndex];

    }
}
