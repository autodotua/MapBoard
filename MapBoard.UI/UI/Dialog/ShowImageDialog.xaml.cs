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
using System.Windows.Input;
using MapBoard.Util;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 显示缩略图对话框
    /// </summary>
    public partial class ShowImageDialog : RightBottomFloatDialogBase
    {
        /// <summary>
        /// 可能需要转换才能显示的文件类型
        /// </summary>
        public static readonly string[] needConvertExtensions = new string[] { ".heif", ".heic", ".avif" };

        /// <summary>
        /// 已经经过转换的源文件和转换后文件的映射
        /// </summary>
        private static Dictionary<string, string> convertedImages = new Dictionary<string, string>();
        private volatile int version = 0;
        public ShowImageDialog(Window owner) : base(owner)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            InitializeComponent();
            this.PropertyChanged += ShowImageDialog_PropertyChanged;
        }

        public Uri ImageUri { get; set; }
        public bool Loading { get; set; }
        protected override int OffsetY => 248;

        private string currentImagePath { get; set; }

        public async Task SetImageAsync(string imagePath)
        {
            currentImagePath = imagePath;
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
                if (Config.Instance.ThumbnailCompatibilityMode
                    && needConvertExtensions.Contains(Path.GetExtension(imagePath).ToLower()))
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
                else
                {
                    ImageUri = new Uri(imagePath);
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

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDoubleClick(e);
            if (currentImagePath != null)
            {
                IOUtility.TryOpenInShellAsync(currentImagePath);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            zb.Reset();
        }

        private void MapView_BoardTaskChanged(object sender, BoardTaskChangedEventArgs e)
        {
            if (e.NewTask is not BoardTask.Select && !IsClosed)
            {
                Close();
            }
        }

        private void ShowImageDialog_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageUri))
            {
                zb.Reset();
            }
        }
    }
}