using FzLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.ViewModels
{
    public class FtpPageViewModels : INotifyPropertyChanged
    {
        private string ip;
        public string IP
        {
            get => ip;
            set => this.SetValueAndNotify(ref ip, value, nameof(IP));
        }

        private bool on = false;
        public bool IsOn
        {
            get => on;
            set => this.SetValueAndNotify(ref on, value, nameof(IsOn));
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
