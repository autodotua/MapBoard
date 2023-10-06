#if ANDROID
using Android.App;
using Android.Net;
using Android.Net.Wifi;
#endif
using FubarDev.FtpServer.FileSystem.DotNet;
using FubarDev.FtpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MapBoard.Services
{
    public class FtpService
    {
        public FtpService()
        {
            var services = new ServiceCollection();

            services.Configure<DotNetFileSystemOptions>(opt => opt.RootPath = FileSystem.Current.AppDataDirectory);

            services.AddFtpServer(builder => builder
                .UseDotNetFileSystem()
                .EnableAnonymousAuthentication());

            services.Configure<FtpServerOptions>(opt =>
            {
                opt.ServerAddress = "0.0.0.0";
                opt.Port = 2222;
            });

            var serviceProvider = services.BuildServiceProvider();

            ftpServerHost = serviceProvider.GetRequiredService<IFtpServerHost>();
        }

        IFtpServerHost ftpServerHost;
        public Task StartServerAsync()
        {


            return ftpServerHost.StartAsync();

        }

        public Task StopServerAsync()
        {
            return ftpServerHost.StopAsync();
        }

        public static IEnumerable<string> GetIpAddress()
        {
#if ANDROID
            var manager = Android.App.Application.Context.GetSystemService(Service.ConnectivityService);
            if (manager is ConnectivityManager m)
            {
                var link = m.GetLinkProperties(m.ActiveNetwork);
                foreach (var address in link.LinkAddresses)
                {
                    yield return address.ToString().Split('/')[0];
                }
            }
#elif WINDOWS
            return null;
#endif
        }
    }
}
