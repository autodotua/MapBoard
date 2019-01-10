using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard
{
    public static class BoardTaskManager
    {
        private static BoardTask currentTask = BoardTask.Ready;
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
        public enum BoardTask
        {
            Ready,
            Draw,
            Edit,
            Select,
        }

        public delegate void BoardTaskChangedEventHandler(object sender, BoardTaskChangedEventArgs e);
        public static event BoardTaskChangedEventHandler BoardTaskChanged;

        public class BoardTaskChangedEventArgs : EventArgs
        {
            public BoardTaskChangedEventArgs(BoardTask oldTask, BoardTask newTask)
            {
                OldTask = oldTask;
                NewTask = newTask;
            }

            public BoardTask OldTask { get; private set; }
            public BoardTask NewTask { get; private set; }

            public bool IsTaskChanged(BoardTask taskType)
            {
                return OldTask == taskType || NewTask == taskType;
            }
        }
    }
}
