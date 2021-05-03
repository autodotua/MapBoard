using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main
{
    public static class BoardTaskManager
    {
        /// <summary>
        /// 画板当前任务
        /// </summary>
        private static BoardTask currentTask = BoardTask.Ready;

        /// <summary>
        /// 画板当前任务
        /// </summary>
        public static BoardTask CurrentTask
        {
            get => currentTask;
            set
            {
                if (currentTask != value)
                {
                    BoardTask oldTask = currentTask;
                    currentTask = value;

                    BoardTaskChanged?.Invoke(null, new BoardTaskChangedEventArgs(oldTask, value));
                }
            }
        }

        /// <summary>
        /// 画板任务类型
        /// </summary>
        public enum BoardTask
        {
            Ready,
            Draw,
            Edit,
            Select,
        }

        public delegate void BoardTaskChangedEventHandler(object sender, BoardTaskChangedEventArgs e);

        public static event BoardTaskChangedEventHandler BoardTaskChanged;

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
            /// 旧任务
            /// </summary>
            public BoardTask OldTask { get; private set; }

            /// <summary>
            /// 新任务
            /// </summary>
            public BoardTask NewTask { get; private set; }
        }
    }
}