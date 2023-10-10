using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace MapBoard.Platforms.Android;

public class AndroidNotificationPermission : BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
      new List<(string androidPermission, bool isRuntime)>
      {
        (global::Android.Manifest.Permission.PostNotifications, true),
      }.ToArray();
}

