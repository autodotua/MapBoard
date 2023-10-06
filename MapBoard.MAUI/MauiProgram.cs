using Esri.ArcGISRuntime.Maui;
using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem.DotNet;
using MapBoard.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;

namespace MapBoard
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.UseArcGISRuntime();
#if DEBUG
    		builder.Logging.AddDebug();
#endif
            FtpService.GetIpAddress();

            //// Setup dependency injection
            ////var services = new ServiceCollection();

            //// use %TEMP%/TestFtpServer as root folder
            //builder.Services.Configure<DotNetFileSystemOptions>(opt => opt
            //    .RootPath = Path.Combine(Path.GetTempPath(), "TestFtpServer"));

            //// Add FTP server services
            //// DotNetFileSystemProvider = Use the .NET file system functionality
            //// AnonymousMembershipProvider = allow only anonymous logins
            //builder.Services.AddFtpServer(builder => builder
            //    //.UseDotNetFileSystem() // Use the .NET file system functionality
            //    .EnableAnonymousAuthentication()); // allow anonymous logins

            //// Configure the FTP server
            //builder.Services.Configure<FtpServerOptions>(opt => {
            //    opt.ServerAddress = "0.0.0.0";
            //    opt.Port = 2222;
            //    });

            //// Build the service provider
            //using (var serviceProvider = builder.Services.BuildServiceProvider())
            //{
            //    // Initialize the FTP server
            //    var ftpServerHost = serviceProvider.GetRequiredService<IFtpServerHost>();

            //    // Start the FTP server
            //    ftpServerHost.StartAsync(CancellationToken.None).ContinueWith(a => Debug.WriteLine("OKKKKK"));

            //    //ftpServerHost.StopAsync(CancellationToken.None).Wait();
            //}




            return builder.Build();
        }
    }
}
