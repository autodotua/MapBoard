using Esri.ArcGISRuntime.Data;
using MapBoard.Common;
using MapBoard.Main.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.IO
{
    internal static class PathUtility
    {
        public static DirectoryInfo GetTempDir()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            DirectoryInfo directory = new DirectoryInfo(tempDirectory);
            if (directory.Exists)
            {
                directory.Delete(true);
            }
            directory.Create();
            return directory;
        }
    }
}