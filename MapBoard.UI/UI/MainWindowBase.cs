using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Threading.Tasks;

namespace MapBoard.UI
{
    /// <summary>
    /// 所有主要窗口的基类
    /// </summary>
    public abstract class MainWindowBase : WindowBase
    {
        /// <summary>
        /// 窗口是否已经初始化
        /// </summary>
        protected bool initialized = false;

        /// <summary>
        /// 程序是否已经初始化。在<see cref="SplashWindow"/>显示时，若窗口初始化失败，则认为程序初始化失败
        /// </summary>
        protected bool programInitialized = false;

        /// <summary>
        /// 配置
        /// </summary>
        public Config Config => Config.Instance;

        /// <summary>
        /// 创建并且显示窗口。如果<see cref="SplashWindow"/>可见，则在后台进行初始化，然后再显示
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="beforeInitialize"></param>
        /// <returns></returns>
        public static async Task<T> CreateAndShowAsync<T>(Action<T> beforeInitialize = null) where T : MainWindowBase, new()
        {
            T win = new T();
            if (SplashWindow.IsShowing)
            {
                win.initialized = true;
                beforeInitialize?.Invoke(win);
                try
                {
                    await win.InitializeAsync();
                    win.programInitialized = true;
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
                SplashWindow.EnsureInvisible();
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

        /// <summary>
        /// 窗口打开、渲染结束后，如果还未初始化，则进行初始化。主窗口会在显示<see cref="SplashWindow"/>时进行初始化，因此此时不会调用初始化。
        /// </summary>
        /// <param name="e"></param>
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
}