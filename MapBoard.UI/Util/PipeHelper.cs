using System;
using System.IO;
using System.Threading.Tasks;
using FzLib;
using FzLib.Program.Runtime;
using MapBoard.UI;
using static FzLib.Program.Runtime.SimplePipe;

namespace MapBoard.Util
{
    public static class PipeHelper
    {
        private static Clinet clinet;

        public static void RegistClinet()
        {
            clinet = new Clinet(FzLib.Program.App.ProgramName);
            clinet.Start();
            clinet.GotMessage += ClinetGotMessage;
        }

        private static async void ClinetGotMessage(object sender, PipeMessageEventArgs e)
        {
            if (e.Message.StartsWith("mbmpkg"))
            {
                string path = e.Message.RemoveStart("mbmpkg ");
                if (File.Exists(path))
                {
                    await App.Current.Dispatcher.Invoke(async () =>
                    {
                        (App.Current.MainWindow as MainWindow).LoadMbmpkg(path);
                    });
                }
            }
        }

        public static async Task Send(string message)
        {
            var server = new Server(FzLib.Program.App.ProgramName);

            await server.SendMessageAsync(message);
        }
    }
}