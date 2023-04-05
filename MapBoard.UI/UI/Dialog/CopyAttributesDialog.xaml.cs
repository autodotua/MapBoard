using Esri.ArcGISRuntime.Data;
using FzLib;
using MapBoard.Mapping.Model;
using MapBoard.Model;
using ModernWpf.FzExtension.CommonDialog;
using System;
using System.Linq;
using System.Windows;

namespace MapBoard.UI.Dialog
{
    /// <summary>
    /// 字段赋值对话框。由于历史原因，类名与实际作用不完全相同。
    /// </summary>
    public partial class CopyAttributesDialog : CommonDialog
    {
        /// <summary>
        /// 源字段
        /// </summary>
        private FieldInfo fieldSource;

        /// <summary>
        /// 目标字段
        /// </summary>
        private FieldInfo fieldTarget;

        public CopyAttributesDialog(IEditableLayerInfo layer)
        {
            Layer = layer;
            Fields = layer.Fields;
            InitializeComponent();
        }

        /// <summary>
        /// 日期格式
        /// </summary>
        public string DateFormat { get; set; } = Parameters.DateFormat;

        /// <summary>
        /// 所有字段
        /// </summary>
        public FieldInfo[] Fields { get; }

        /// <summary>
        /// 图层
        /// </summary>
        public IEditableLayerInfo Layer { get; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 源字段
        /// </summary>
        public FieldInfo SourceField
        {
            get => fieldSource;
            set
            {
                fieldSource = value;
                UpdateMessage();
            }
        }

        /// <summary>
        /// 目标字段
        /// </summary>
        public FieldInfo TargetField
        {
            get => fieldTarget;
            set
            {
                fieldTarget = value;
                UpdateMessage();
            }
        }

        /// <summary>
        /// 常量文本
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 字段赋值的类型
        /// </summary>
        public FieldAssignmentType Type { get; set; }

        private void CommonDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            Type = (FieldAssignmentType)tab.SelectedIndex;
        }
        
        /// <summary>
        /// 选择的选项卡发生改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tab_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateMessage();
        }

        /// <summary>
        /// 更新提示信息
        /// </summary>
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

    }
}