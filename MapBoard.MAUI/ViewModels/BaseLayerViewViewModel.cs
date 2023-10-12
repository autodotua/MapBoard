using MapBoard.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class BaseLayerViewViewModel : INotifyPropertyChanged
    {
        public BaseLayerViewViewModel()
        {
            BaseLayers = new ObservableCollection<BaseLayerInfo>(Config.Instance.BaseLayers); ;
        }

        public void Save()
        {
            Config.Instance.BaseLayers = new List<BaseLayerInfo>(BaseLayers);
            Config.Instance.Save();
        }

        public ObservableCollection<BaseLayerInfo> BaseLayers { get; set; } = new ObservableCollection<BaseLayerInfo>();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
