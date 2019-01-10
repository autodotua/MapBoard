using FzLib.Basic.Collection;
using FzLib.Control.Dialog;
using FzLib.Program;
using MapBoard.Style;
using MapBoard.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Format
{
    public static class Mbpkg
    {
        public static void Import(string path)
        {
            StyleCollection.Instance.Styles.ForEach(p => p.Table.Close());
            if (Directory.Exists(Config.DataPath))
            {
                Directory.Delete(Config.DataPath, true);
            }
            ZipFile.ExtractToDirectory(path, Config.DataPath);

            Information.Restart();
        }

        public static async void Export(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            StyleCollection.Instance.Styles.ForEach(p => p.Table.Close());
            ZipFile.CreateFromDirectory(Config.DataPath, path);
            await ArcMapView.Instance.LoadLayers();
        }
    }
}
