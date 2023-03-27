using FzLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Model
{
    public class TileSourceCollection : INotifyPropertyChanged
    {
        public TileSourceCollection()
        {
            Sources.CollectionChanged += (p1, p2) =>
              {
                  if (p2.NewItems != null)
                  {
                      (p2.NewItems[0] as BaseLayerInfo).PropertyChanged += (p3, p4) =>
                      {
                          if (p3 == SelectedUrl)
                          {
                              this.Notify(nameof(SelectedUrl));
                          }
                      };
                  }
              };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int SelectedIndex { get; set; }
        public BaseLayerInfo SelectedUrl => SelectedIndex == -1 || SelectedIndex >= Sources.Count ? null : Sources[SelectedIndex];
        public ObservableCollection<BaseLayerInfo> Sources { get; set; } = new ObservableCollection<BaseLayerInfo>();
    }
}