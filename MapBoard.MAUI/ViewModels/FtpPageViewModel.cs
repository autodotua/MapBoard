using FzLib;
using MapBoard.Mapping.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.ViewModels
{
    public class FtpPageViewModel : INotifyPropertyChanged
    {
        private string ip;
        private bool on = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public string IP
        {
            get => ip;
            set => this.SetValueAndNotify(ref ip, value, nameof(IP));
        }
        public bool IsOn
        {
            get => on;
            set => this.SetValueAndNotify(ref on, value, nameof(IsOn));
        }
    }
}
