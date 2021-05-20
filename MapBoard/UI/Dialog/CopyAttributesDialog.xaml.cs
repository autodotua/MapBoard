using FzLib.Extension;
using FzLib.UI.Dialog;
using FzLib.UI.Extension;
using MapBoard.Common.Dialog;
using MapBoard.Main.Model;
using MapBoard.Main.Util;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MapBoard.Main.UI.Dialog
{
    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CopyAttributesDialog : CommonDialog
    {
        public LayerInfo Layer { get; }

        public CopyAttributesDialog(LayerInfo layer)
        {
            Layer = layer;
            Fields = layer.Fields.IncludeDefaultFields().ToArray();
            InitializeComponent();
        }

        public FieldInfo[] Fields { get; }
        private FieldInfo fieldSource;

        public FieldInfo FieldSource
        {
            get => fieldSource;
            set
            {
                this.SetValueAndNotify(ref fieldSource, value, nameof(FieldSource));
                UpdateMessage();
            }
        }

        private FieldInfo fieldTarget;

        public FieldInfo FieldTarget
        {
            get => fieldTarget;
            set
            {
                this.SetValueAndNotify(ref fieldTarget, value, nameof(FieldTarget));
                UpdateMessage();
            }
        }

        private string message;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        private string dateFormat = "yyyy-MM-dd";

        public string DateFormat
        {
            get => dateFormat;
            set => this.SetValueAndNotify(ref dateFormat, value, nameof(DateFormat));
        }

        private void UpdateMessage()
        {
            try
            {
                if (FieldSource == null || FieldTarget == null)
                {
                    throw new Exception("");
                }
                if (FieldSource == FieldTarget)
                {
                    throw new Exception("");
                }
                if (FieldSource.Type != FieldTarget.Type)
                {
                    if (FieldSource.Type == FieldInfoType.Date)
                    {
                        if (FieldTarget.Type == FieldInfoType.Integer || FieldTarget.Type == FieldInfoType.Float)
                        {
                            throw new Exception("数值与日期之间不可互转");
                        }
                    }
                    if (FieldTarget.Type == FieldInfoType.Date)
                    {
                        if (FieldSource.Type == FieldInfoType.Integer || FieldSource.Type == FieldInfoType.Float)
                        {
                            throw new Exception("数值与日期之间不可互转");
                        }
                    }
                }
                IsPrimaryButtonEnabled = true;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                IsPrimaryButtonEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
        }
    }
}