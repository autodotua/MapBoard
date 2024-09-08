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
    public static bool IsRunning { get; private set; }
    private readonly string NotificationChannelID = "MapBoard.Track";
    private readonly string NotificationChannelName = "轨迹记录";
    private readonly int NotificationID = 1;
    private IBinder binder;
    private NotificationCompat.Builder notificationBuilder;
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
    private bool isStopping = false;
    private NotificationManager notificationManager;
    public override void OnCreate()
    {
        base.OnCreate();
        notificationManager = GetSystemService(NotificationService) as NotificationManager;
    }
    public override void OnDestroy()
    {
        IsRunning = false;
        isStopping = true;
        base.OnDestroy();
        if (TrackService.Current != null)
        {
            TrackService.Stop();
        }
        timer.Stop();
        TrackService.CurrentChanged -= TrackService_CurrentChanged;
        TrackService = null;
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        IsRunning = true;
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
        TrackService.CurrentChanged += TrackService_CurrentChanged;

        trackService.Start();

        timer = App.Current.Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(Config.Instance.TrackNotificationUpdateTimeSpan);
        timer.Tick += Timer_Tick;
        timer.Start();
        UpdateNotification();
    }

    IDispatcherTimer timer;

    private void Timer_Tick(object sender, EventArgs e)
    {
        UpdateNotification();
    }

    private void UpdateNotification()
    {
        if (notificationBuilder == null || TrackService == null)
        {
            return;
        }
        notificationBuilder.SetContentTitle(pausing ? "暂停记录轨迹" : "正在记录轨迹");
        notificationBuilder.SetContentText($"用时{DateTime.Now - TrackService.StartTime:hh':'mm':'ss}，总长度{TrackService.TotalDistance:0}米");
        notificationManager.Notify(NotificationID, notificationBuilder.Build());
    }

    private void TrackService_CurrentChanged(object sender, EventArgs e)
    {
        if (TrackService.Current == null && !isStopping)
        {
            StopSelf();
        }
    }

    private void StartForegroundService()
    {
        Intent intent = new Intent(this, typeof(MainActivity));

        const int pendingIntentId = 0;
        PendingIntent pendingIntent =
            PendingIntent.GetActivity(this, pendingIntentId, intent, PendingIntentFlags.Immutable);

        notificationBuilder = new NotificationCompat.Builder(this, NotificationChannelID)
            .SetAutoCancel(false)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .SetSmallIcon(Resource.Drawable.btn_track)
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
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationID, notificationBuilder.Build(), ForegroundService.TypeLocation);
        }
        else
        {
            StartForeground(NotificationID, notificationBuilder.Build());
        }
        notificationManager.Notify(NotificationID, notificationBuilder.Build());
    }
}

