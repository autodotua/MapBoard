#if ANDROID
using Android.App;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Util
{
    public static class PlatformDialog
    {
        public static object ShowLoading(string message)
        {
#if ANDROID
            //使用Platform.Current时，如果刚好从外部程序（Files）跳转过来，会得到外部的Activity造成问题。
            ProgressDialog dialog = ProgressDialog.Show(MainActivity.Current, "", message, true);
            return dialog;
#else
            throw new NotImplementedException();
#endif
        }

        public static void DismissLoading(object handle)
        {
#if ANDROID
            var dialog = (ProgressDialog)handle;
            try
            {
                dialog.Dismiss();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
#else
            throw new NotImplementedException();
#endif
        }
    }
}
