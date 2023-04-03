using System;
using System.IO;
using System.Threading.Tasks;
using FzLib;
using FzLib.Program.Runtime;
using MapBoard.UI;
using static FzLib.Program.Runtime.SimplePipe;

namespace MapBoard.Util
{
    /// <summary>
    /// 进程间通信的管道帮助类
    /// </summary>
    public static class PipeHelper
    {
        private static Clinet clinet;

        /// <summary>
        /// 注册客户端
        /// </summary>
        public static void RegistClinet()
        {
            clinet = new Clinet(FzLib.Program.App.ProgramName);
            clinet.Start();
            clinet.GotMessage += ClinetGotMessage;
        }

        /// <summary>
        /// 客户端收到其他客户端的信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ClinetGotMessage(object sender, PipeMessageEventArgs e)
        {
            if (e.Message.StartsWith("mbmpkg"))
            {
                string path = e.Message.RemoveStart("mbmpkg ");
                if (File.Exists(path))
                {
                    App.Current.Dispatcher.Invoke(() =>
                  {
                      (App.Current.MainWindow as MainWindow).LoadMbmpkg(path);
                  });
                }
            }
        }

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Send(string message)
        {
            var server = new Server(FzLib.Program.App.ProgramName);

            await server.SendMessageAsync(message);
        }
    }
}