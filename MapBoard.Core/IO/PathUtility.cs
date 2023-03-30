using System.IO;

namespace MapBoard.IO
{
    /// <summary>
    /// 路径工具
    /// </summary>
    internal static class PathUtility
    {
        /// <summary>
        /// 获取临时目录
        /// </summary>
        /// <returns></returns>
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