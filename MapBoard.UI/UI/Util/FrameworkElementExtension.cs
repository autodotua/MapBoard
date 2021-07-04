using System.Threading.Tasks;
using System.Windows;

namespace MapBoard.UI.Util
{
    public static class FrameworkElementExtension
    {
        public async static Task WaitForLoadedAsync(this FrameworkElement element)
        {
            if (element.IsLoaded)
            {
                return;
            }
            TaskCompletionSource tcs = new TaskCompletionSource();
            element.Loaded += (p1, p2) =>
            {
                tcs.TrySetResult();
            };
            await tcs.Task;
        }
    }
}