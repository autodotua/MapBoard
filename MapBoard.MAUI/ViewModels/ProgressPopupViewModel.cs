using FzLib;
using System.ComponentModel;

namespace MapBoard.ViewModels
{
    public class ProgressPopupViewModel : INotifyPropertyChanged
    {
        private string message;

        public event PropertyChangedEventHandler PropertyChanged;
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

    }
}
