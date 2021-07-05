using ModernWpf;
using ModernWpf.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace MapBoard.UI
{
    public abstract class MainWindowBase : WindowBase
    {
        public static async Task<T> CreateAndShowAsync<T>() where T : MainWindowBase, new()
        {
            T win = new T();
            if (SplashWindow.IsVisiable)
            {
                win.initialized = true;
                try
                {
                    await win.InitializeAsync();
                }
                catch (Exception ex)
                {
                    win.Loaded += (s, e) =>
                    {
                        CommonDialog.ShowErrorDialogAsync(ex, "初始化失败，程序将无法正常运行").ConfigureAwait(false);
                    };
                }
                win.Show();
                SplashWindow.EnsureInvisiable();
            }
            else
            {
                win.Show();
            }
            return win;
        }

        protected async override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (initialized)
            {
                return;
            }
            await DoAsync(async () =>
            {
                try
                {
                    await InitializeAsync();
                }
                catch (Exception ex)
                {
                    CommonDialog.ShowErrorDialogAsync(ex, "初始化失败，程序将无法正常运行");
                }
            }, "正在初始化", delay: 0);
        }

        protected bool initialized = false;

        /// <summary>
        /// 用于处理一些窗口在初始化后需要加载的耗时异步操作
        /// </summary>
        /// <returns></returns>
        protected abstract Task InitializeAsync();
    }

    public abstract class WindowBase : Window, INotifyPropertyChanged
    {
        public WindowBase()
        {
            DataContext = this;
            WindowCreated?.Invoke(this, EventArgs.Empty);
        }

        public static event EventHandler WindowCreated;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsClosed { get; private set; }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }

        public void BringToFront()
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            Topmost = true;  // important
            Topmost = false; // important
            Focus();
        }

        /// <summary>
        /// 显示处理中遮罩并处理需要长时间运行的方法
        /// </summary>
        /// <param name="action"></param>
        /// <param name="catchException"></param>
        /// <returns></returns>
        public Task DoAsync(Func<Task> action, string message, bool catchException = false, int delay = 0)
        {
            return DoAsync(async p => await action(), message, catchException, delay);
        }

        private ProgressRingOverlay loading = null;

        protected void ShowLoading(int delay, string message)
        {
            if (loading == null)
            {
                if (Content is Grid grd)
                {
                    loading = new ProgressRingOverlay();
                    Grid.SetColumnSpan(loading, int.MaxValue);
                    Grid.SetRowSpan(loading, int.MaxValue);
                    loading.Margin = new Thickness(
                        -grd.Margin.Left,
                        -grd.Margin.Top,
                        -grd.Margin.Right,
                        -grd.Margin.Bottom);
                    Panel.SetZIndex(loading, int.MaxValue);
                    grd.Children.Add(loading);
                }
                else
                {
                    throw new NotSupportedException("不支持非Grid的窗口内容布局");
                }
            }
            loading.Message = message;
            loading.Show(delay);
        }

        protected void HideLoading()
        {
            loading?.Hide();
        }

        /// <summary>
        /// 显示处理中遮罩并处理需要长时间运行的方法
        /// </summary>
        /// <param name="action"></param>
        /// <param name="catchException"></param>
        /// <returns></returns>
        public async Task DoAsync(Func<ProgressRingOverlayArgs, Task> action, string message, bool catchException = false, int delay = 500)
        {
            ShowLoading(delay, message);
            try
            {
                await action(loading.TaskArgs);
            }
            catch (Exception ex)
            {
                if (catchException)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                HideLoading();
            }
        }
    }
}