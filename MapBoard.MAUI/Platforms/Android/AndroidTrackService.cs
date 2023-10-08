using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Java.Interop;
using MapBoard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Platforms.Android;

[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class AndroidTrackService : Service
{
    private readonly string NotificationChannelID = "MapBoard.Track";
    private readonly string NotificationChannelName = "轨迹记录";
    private readonly int NotificationID = 1;
    private IBinder binder;
    NotificationCompat.Builder notificationBuilder;
    private bool pausing = false;

    public AndroidTrackService()
    {
        binder = new AndroidTrackServiceBinder(this);
    }

    public TrackService TrackService { get; private set; }

    public override IBinder OnBind(Intent intent)
    {
        return binder;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        TrackService.Stop();
        TrackService.LocationChanged -= TrackService_LocationChanged;
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        StartForegroundService();
        return StartCommandResult.NotSticky;
    }

    public void SetTrackServiceAndStart(TrackService trackService)
    {
        if (TrackService != null)
        {
            throw new Exception("不可重复设置");
        }

        TrackService = trackService;
        TrackService.LocationChanged += TrackService_LocationChanged;
        trackService.Start();
    }
    private void StartForegroundService()
    {
        var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;

        Intent intent = new Intent(this, typeof(MainActivity));

        const int pendingIntentId = 0;
        PendingIntent pendingIntent =
            PendingIntent.GetActivity(this, pendingIntentId, intent, PendingIntentFlags.Immutable);

        notificationBuilder = new NotificationCompat.Builder(this, NotificationChannelID)
            .SetAutoCancel(false)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .SetSmallIcon(Resource.Drawable.tab_track)
            .SetContentTitle("正在记录轨迹")
            .SetContentText("等待定位");
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            NotificationChannel notificationChannel = new NotificationChannel(NotificationChannelID,
                    NotificationChannelName, NotificationImportance.Low);
            notificationChannel.EnableLights(false);
            notificationChannel.SetShowBadge(true);
            notificationChannel.LockscreenVisibility = NotificationVisibility.Public;
            notificationManager.CreateNotificationChannel(notificationChannel);
            notificationBuilder.SetChannelId(NotificationChannelID);
            StartForeground(NotificationID, notificationBuilder.Build(), ForegroundService.TypeLocation);
        }
        else
        {
            StartForeground(NotificationID, notificationBuilder.Build());
        }
        notificationManager.Notify(NotificationID, notificationBuilder.Build());
    }

    private void TrackService_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        notificationBuilder.SetContentTitle(pausing ? "暂停记录轨迹" : "正在记录轨迹");

        notificationBuilder.SetContentText($"用时{DateTime.Now - TrackService.StartTime:hh':'mm':'ss}，总长度{TrackService.TotalDistance:0}米");

        if (GetSystemService(Context.NotificationService) is NotificationManager notificationManager)
        {
            notificationManager.Notify(NotificationID, notificationBuilder.Build());
        }

    }
}

public class AndroidTrackServiceBinder : Binder
{
    public AndroidTrackServiceBinder(AndroidTrackService service)
    {
        Service = service;
    }

    public AndroidTrackService Service { get; }
}
