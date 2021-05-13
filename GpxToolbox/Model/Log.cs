using System.Collections.ObjectModel;

namespace MapBoard.GpxToolbox.Model
{
    public static class Log
    {
        public static ObservableCollection<string> ErrorLogs { get; set; } = new ObservableCollection<string>();
    }
}