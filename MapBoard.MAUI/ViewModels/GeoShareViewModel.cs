using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using MapBoard.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.ViewModels
{
    public partial class GeoShareViewModel : ObservableObject
    {
        public GeoShareConfig Config => MapBoard.Config.Instance.GeoShare;

        [ObservableProperty]
        private bool isReady = true;

        public void NotifyConfig()
        {
            this.Notify(nameof(Config));
        }
    }
}
