using FzLib.Extension;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Linq;
using System.Windows;

namespace MapBoard.UI.Dialog
{
    public enum CopyAttributesType
    {
        Field = 0,
        Const = 1,
        Custom = 2
    }

    /// <summary>
    /// SelectStyleDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CopyAttributesDialog : CommonDialog
    {
        public IWriteableLayerInfo Layer { get; }

        public CopyAttributesDialog(IWriteableLayerInfo layer)
        {
            Layer = layer;
            Fields = layer.Fields.IncludeDefaultFields().ToArray();
            InitializeComponent();
        }

        public FieldInfo[] Fields { get; }
        private FieldInfo fieldSource;

        public FieldInfo SourceField
        {
            get => fieldSource;
            set
            {
                this.SetValueAndNotify(ref fieldSource, value, nameof(SourceField));
                UpdateMessage();
            }
        }

        private FieldInfo fieldTarget;

        public FieldInfo TargetField
        {
            get => fieldTarget;
            set
            {
                this.SetValueAndNotify(ref fieldTarget, value, nameof(TargetField));
                UpdateMessage();
            }
        }

        private string text;

        public string Text
        {
            get => text;
            set => this.SetValueAndNotify(ref text, value, nameof(Text));
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
                if (tab.SelectedIndex == 0)
                {
                    if (SourceField == null || TargetField == null)
                    {
                        throw new Exception("");
                    }
                    if (SourceField == TargetField)
                    {
                        throw new Exception("");
                    }
                    if (SourceField.Type != TargetField.Type)
                    {
                        if (SourceField.Type is FieldInfoType.Date or FieldInfoType.Time)
                        {
                            if (TargetField.Type == FieldInfoType.Integer || TargetField.Type == FieldInfoType.Float)
                            {
                                throw new Exception("数值与日期之间不可互转");
                            }
                        }
                        if (TargetField.Type is FieldInfoType.Date or FieldInfoType.Time)
                        {
                            if (SourceField.Type == FieldInfoType.Integer || SourceField.Type == FieldInfoType.Float)
                            {
                                throw new Exception("数值与日期之间不可互转");
                            }
                        }
                    }
                }
                else
                {
                    if (TargetField == null)
                    {
                        throw new Exception("");
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

        public CopyAttributesType Type { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            Type = (CopyAttributesType)tab.SelectedIndex;
        }

        private void tab_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateMessage();
        }
    }
}