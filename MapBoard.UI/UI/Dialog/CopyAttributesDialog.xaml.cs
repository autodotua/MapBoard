using FzLib;
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
        private FieldInfo fieldSource;
        private FieldInfo fieldTarget;

        public CopyAttributesDialog(IEditableLayerInfo layer)
        {
            Layer = layer;
            Fields = layer.Fields;
            InitializeComponent();
        }

        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public FieldInfo[] Fields { get; }
        public IEditableLayerInfo Layer { get; }
        public string Message { get; set; }

        public FieldInfo SourceField
        {
            get => fieldSource;
            set
            {
                fieldSource = value;
                UpdateMessage();
            }
        }

        public FieldInfo TargetField
        {
            get => fieldTarget;
            set
            {
                fieldTarget = value;
                UpdateMessage();
            }
        }

        public string Text { get; set; }
        public CopyAttributesType Type { get; set; }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            Type = (CopyAttributesType)tab.SelectedIndex;
        }

        private void tab_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateMessage();
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
                App.Log.Error(ex);
                Message = ex.Message;
                IsPrimaryButtonEnabled = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}