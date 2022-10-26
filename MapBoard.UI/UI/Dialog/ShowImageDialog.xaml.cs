using Esri.ArcGISRuntime.Data;
using MapBoard.Mapping;
using MapBoard.UI.Bar;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Linq;
using MapBoard.Mapping.Model;
using FzLib;
using MapBoard.IO;
using System.Runtime.CompilerServices;
using System.IO;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ShowImageDialog : RightBottomFloatDialogBase
    {
        private static Dictionary<string, string> convertedImages = new Dictionary<string, string>();
        public Uri ImageUri { get; set; }
        public bool Loading { get; set; }

        public ShowImageDialog(Window owner) : base(owner)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            InitializeComponent();
        }

        private volatile int version = 0;
        public async Task SetImageAsync(string imagePath)
        {
            ImageUri = null;
            if (imagePath == null)
            {
                return;
            }
            Loading = true;

            version++;
            int currentVersion = version;
            try
            {
                string convertedImagePath = null;
                //查询缓存
                if (convertedImages.ContainsKey(imagePath) && File.Exists(convertedImages[imagePath]))
                {
                    convertedImagePath = convertedImages[imagePath];
                }
                else
                {
                    PresentationSource source = PresentationSource.FromVisual(this);
                    convertedImagePath = await Photo.GetDisplayableImage(imagePath,
                       (int)Math.Max(ActualHeight * source.CompositionTarget.TransformToDevice.M22,
                       ActualWidth * source.CompositionTarget.TransformToDevice.M11));
                    convertedImages.Add(imagePath, convertedImagePath);
                }

                //由于转换时间比较长，如果在转换时又选择了另一张图片，就会导致问题，所以需要保证没有选择新的突破
                if (currentVersion == version)
                {
                    ImageUri = new Uri(convertedImagePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"无法加载{imagePath}：{ex.Message}");
            }
            finally
            {
                if (currentVersion == version)
                {
                    Loading = false;
                }
            }
        }
        protected override int OffsetY => 248;

        private void MapView_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask is not BoardTask.Select && !IsClosed)
            {
                Close();
            }
        }

    }
}