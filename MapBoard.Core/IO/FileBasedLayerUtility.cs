using System.IO;
using MapBoard.Mapping.Model;

namespace MapBoard.IO
{
    public class FileBasedLayerUtility
    {
        public static void CopyLayerFiles(string directory, IFileBasedLayer layer)
        {
            var files = layer.GetFilePaths();
            foreach (var file in files)
            {
                File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
            }
        }
    }
}