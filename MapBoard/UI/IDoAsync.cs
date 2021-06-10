using ModernWpf.FzExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.UI
{
    public interface IDoAsync
    {
        public Task DoAsync(Func<ProgressRingOverlayArgs, Task> action, string message, bool catchException = false);

        public Task DoAsync(Func<Task> action, string message, bool catchException = false);
    }
}