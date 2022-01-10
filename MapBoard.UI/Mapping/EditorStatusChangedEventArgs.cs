using System;

namespace MapBoard.Mapping
{
    public class EditorStatusChangedEventArgs : EventArgs
    {
        public EditorStatusChangedEventArgs(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public bool IsRunning { get; set; }
    }
}