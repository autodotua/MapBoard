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
        protected bool initialized = false;
        protected bool programInitialized = false;

        public static async Task<T> CreateAndShowAsync<T>(Action<T> beforeInitialize = null) where T : MainWindowBase, new()
        {
            T win = new T();
            if (SplashWindow.IsVisiable)
            {
                win.initialized = true;
                beforeInitialize?.Invoke(win);
                try
                {
                    await win.InitializeAsync();
                }
                catch (Exception ex)
                {
                    App.Log.Error("窗口初始化失败", ex);
                    win.programInitialized = false;
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

        /// <summary>
        /// 用于处理一些窗口在初始化后需要加载的耗时异步操作
        /// </summary>
        /// <returns></returns>
        protected abstract Task InitializeAsync();

        protected override async void OnContentRendered(EventArgs e)
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
                    App.Log.Error("窗口初始化失败", ex);
                    CommonDialog.ShowErrorDialogAsync(ex, "初始化失败，程序将无法正常运行");
                }
            }, "正在初始化", delay: 0);
        }
    }

    public abstract class WindowBase : Window, INotifyPropertyChanged
    {
        private ProgressRingOverlay loading = null;

        public WindowBase()
        {
            DataContext = this;
            WindowCreated?.Invoke(this, EventArgs.Empty);
        }

        public static event EventHandler WindowCreated;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsClosed { get; private set; }

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

        protected void HideLoading()
        {
            loading?.Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }

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
    }
}