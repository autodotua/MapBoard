using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.GpxToolbox
{
  public static class Log
    {
      public static  ObservableCollection<string> ErrorLogs { get; set; } = new ObservableCollection<string>();
    }
}
