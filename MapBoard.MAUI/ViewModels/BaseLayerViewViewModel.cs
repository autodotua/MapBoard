using FzLib;
using MapBoard.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class BaseLayerViewViewModel : INotifyPropertyChanged
    {
        public BaseLayerViewViewModel()
        {
            BaseLayers = new ObservableCollection<BaseLayerInfo>(Config.Instance.BaseLayers);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<BaseLayerInfo> BaseLayers { get; set; }

        public void Save()
        {
            Config.Instance.BaseLayers = new List<BaseLayerInfo>(BaseLayers);
            Config.Instance.Save();
        }
    }
}
