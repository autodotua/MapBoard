using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Platforms.Android
{
    [Service(ForegroundServiceType = ForegroundService.TypeLocation)]
    class AndroidTrackService : Service
    {
        private readonly string NotificationChannelID = "MapBoard.Track";
        private readonly int NotificationID = 1;
        private readonly string NotificationChannelName = "track";
        NotificationCompat.Builder notificationBuilder;
        private void StartForegroundService()
        {
            var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {

            }
            notificationBuilder = new NotificationCompat.Builder(this, NotificationChannelID);
            notificationBuilder.SetAutoCancel(false);
            notificationBuilder.SetOngoing(true);
            notificationBuilder.SetSmallIcon(Resource.Drawable.tab_track);
            notificationBuilder.SetContentTitle("正在记录轨迹");
            notificationBuilder.SetContentText("正在记录轨迹");
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel notificationChannel = new NotificationChannel(NotificationChannelID,
                        NotificationChannelName, NotificationImportance.Low);
                notificationChannel.EnableLights(false);
                notificationChannel.SetShowBadge(true);
                notificationChannel.LockscreenVisibility = NotificationVisibility.Public;
                notifcationManager.CreateNotificationChannel(notificationChannel);
                notificationBuilder.SetChannelId(NotificationChannelID);
                StartForeground(NotificationID, notificationBuilder.Build(), ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NotificationID, notificationBuilder.Build());
            }
        }

        private void CreateNotificationChannel(NotificationManager notificationMnaManager)
        {
            var channel = new NotificationChannel(NotificationChannelID, NotificationChannelName,
            NotificationImportance.Low);
            notificationMnaManager.CreateNotificationChannel(channel);


        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }


        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            StartForegroundService();
            StartTimer();
            return StartCommandResult.NotSticky;
        }

        private void StartTimer()
        {
            var timer = Dispatcher.GetForCurrentThread().CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            notificationBuilder.SetContentText(DateTime.Now.ToString());

            var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;
            notifcationManager.Notify(NotificationID, notificationBuilder.Build());
            //await MainThread.InvokeOnMainThreadAsync(() =>
            //   {
            //       notificationBuilder.NotifyAll();
            //   });
        }
    }
}
