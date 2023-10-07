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
        private readonly string NOTIFICATION_CHANNEL_ID = "mapboard.track";
        private readonly int NOTIFICATION_ID = 1;
        private readonly string NOTIFICATION_CHANNEL_NAME = "track";

        private void StartForegroundService()
        {
            var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                CreateNotificationChannel(notifcationManager);
            }
            var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
            notification.SetAutoCancel(false);
            notification.SetOngoing(true);
            notification.SetSmallIcon(Resource.Mipmap.appicon);
            notification.SetContentTitle("ForegroundService");
            notification.SetContentText("Foreground Service is running");
            if ((int)Build.VERSION.SdkInt < 29)
            {
                StartForeground(NOTIFICATION_ID, notification.Build());
            }
            else
            {
                StartForeground(NOTIFICATION_ID, notification.Build(), ForegroundService.TypeLocation);
            }
        }

        private void CreateNotificationChannel(NotificationManager notificationMnaManager)
        {
            var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME,
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
            return StartCommandResult.NotSticky;
        }
    }
}
