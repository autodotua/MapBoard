using System;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 画板任务改变事件参数
    /// </summary>
    public class BoardTaskChangedEventArgs : EventArgs
    {
        public BoardTaskChangedEventArgs(BoardTask oldTask, BoardTask newTask)
        {
            OldTask = oldTask;
            NewTask = newTask;
        }

        /// <summary>
        /// 新任务
        /// </summary>
        public BoardTask NewTask { get; private set; }

        /// <summary>
        /// 旧任务
        /// </summary>
        public BoardTask OldTask { get; private set; }
    }
}