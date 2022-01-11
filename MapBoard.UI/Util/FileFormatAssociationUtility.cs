using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace MapBoard.Util
{
    public class FileFormatAssociationUtility
    {
        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        public static bool SetAssociation(string extension, string id, string fileTypeDescription, string iconPath)
        {
            return SetAssociation(extension, id, fileTypeDescription, iconPath, FzLib.Program.App.ProgramFilePath);
        }

        public static bool SetAssociation(string extension, string id, string fileTypeDescription, string iconPath = null, string applicationFilePath = null)
        {
            if (applicationFilePath == null)
            {
                applicationFilePath = FzLib.Program.App.ProgramFilePath;
            }
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            bool madeChanges = false;
            madeChanges |= SetDefaultValue(@"Software\Classes\" + extension, id);
            madeChanges |= SetDefaultValue(@"Software\Classes\" + id, fileTypeDescription);
            madeChanges |= SetDefaultValue($@"Software\Classes\{id}\shell\open\command", "\"" + applicationFilePath + "\" \"%1\"");
            if (iconPath != null)
            {
                madeChanges |= SetDefaultValue(@"Software\Classes\" + id + "\\DefaultIcon", iconPath);
            }
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            return madeChanges;
        }

        public static void DeleteAssociation(string extension, string id)
        {
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes"))
            {
                if (key.OpenSubKey(extension) != null)
                {
                    key.OpenSubKey(extension, true).DeleteValue(null);
                }
                if (key.OpenSubKey(id) != null)
                {
                    key.DeleteSubKeyTree(id);
                }
            }
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        }

        public static bool IsAssociated(string extension, string id)
        {
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            bool yes = true;
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes"))
            {
                if (!(key.OpenSubKey(extension) != null && key.OpenSubKey(extension) != null && key.OpenSubKey(extension).GetValue(null) as string == id))
                {
                    yes = false;
                }
                if (key.OpenSubKey(id) == null)
                {
                    yes = false;
                }
            }
            return yes;
        }

        private static bool SetDefaultValue(string keyPath, string value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                if (key.GetValue(null) as string != value)
                {
                    key.SetValue(null, value);
                    return true;
                }
            }

            return false;
        }
    }
}