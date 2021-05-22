using System.IO;

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