using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FzLib;
using FzLib.Program.Runtime;
using FzLib.WPF;
using JKang.IpcServiceFramework.Client;
using JKang.IpcServiceFramework.Hosting;
using MapBoard.UI;
using MapBoard.UI.GpxToolbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static FzLib.Program.Runtime.SimplePipe;

namespace MapBoard.Util
{
    /// <summary>
    /// 进程间通信的管道帮助类
    /// </summary>
    public static class PipeHelper
    {
        public interface IPipeService
        {
            void LoadGpxs(IEnumerable<string> files);
        }
        public class PipeService : IPipeService
        {
            public async void LoadGpxs(IEnumerable<string> files)
            {
                await App.Current.Dispatcher.InvokeAsync(async () =>
                       {
                           GpxWindow gpxWindow;
                           if (App.Current.Windows.OfType<GpxWindow>().Any())
                           {
                               gpxWindow = App.Current.Windows.OfType<GpxWindow>().First();
                           }
                           else
                           {
                               gpxWindow = await MainWindowBase.CreateAndShowAsync<GpxWindow>();
                           }
                           gpxWindow.BringToFront();
                           await gpxWindow.LoadGpxFilesAsync(files);
                       });
            }
        }
        private static IHost host;
        public static void StartHost()
        {
            host = Host.CreateDefaultBuilder()
           .ConfigureServices(services =>
           {
               services.AddSingleton<IPipeService, PipeService>();
           })
           .ConfigureIpcHost(builder =>
           {
               //用命名管道会有问题：Kang.IpcServiceFramework.IpcCommunicationException: Invalid message header length must be 4 but was 0
               builder.AddTcpEndpoint<IPipeService>(IPAddress.Loopback, 23456);
           })
           .ConfigureLogging(builder =>
           {
               builder.SetMinimumLevel(LogLevel.Debug);
           })
           .Build();
            host.Start();
        }

        public static async Task LoadGpxs(IEnumerable<string> files)
        {
            ServiceProvider serviceProvider = new ServiceCollection()
                .AddTcpIpcClient<IPipeService>("c", IPAddress.Loopback, 23456)
                .BuildServiceProvider();
            IIpcClientFactory<IPipeService> clientFactory = serviceProvider.GetRequiredService<IIpcClientFactory<IPipeService>>();
            IIpcClient<IPipeService> client = clientFactory.CreateClient("c");
            await client.InvokeAsync(p => p.LoadGpxs(files));
            serviceProvider.Dispose();
        }
    }
}