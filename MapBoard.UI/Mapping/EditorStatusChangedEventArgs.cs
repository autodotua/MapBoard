using System;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 图形编辑器状态改变事件
    /// </summary>
    public class EditorStatusChangedEventArgs : EventArgs
    {
        public EditorStatusChangedEventArgs(bool isRunning)
        {
            IsRunning = isRunning;
        }

        /// <summary>
        /// 是否正在编辑状态
        /// </summary>
        public bool IsRunning { get; set; }
    }
}