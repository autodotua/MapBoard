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
            StyleCollection.Instance.Styles.Clear();
            if (Directory.Exists(Config.DataPath))
            {
                Directory.Delete(Config.DataPath, true);
            }
            ZipFile.ExtractToDirectory(path, Config.DataPath);

            //Information.Restart();
            StyleCollection.ResetStyles();
        }

        public static  void Export(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            StyleCollection.Instance.Save();
          var styles = StyleCollection.Instance.Styles.ToArray();
            StyleCollection.Instance.Styles.Clear();
            styles.ForEach(p => p.Table = null);
            ZipFile.CreateFromDirectory(Config.DataPath, path);
            styles.ForEach(p => StyleCollection.Instance.Styles.Add(p));
        }
    }
}
