using System.ComponentModel;
using System.Diagnostics;

namespace MapBoard.Model
{
    /// <summary>
    /// 导出图层时的字段信息
    /// </summary>
    [DebuggerDisplay("Name={Name} Disp={DisplayName} Type={Type}")]
    public class ExportingFieldInfo : FieldInfo
    {
        public ExportingFieldInfo(string name, string displayName, FieldInfoType type)
        {
            Name = name;
            DisplayName = displayName;
            Type = type;
        }

        public ExportingFieldInfo()
        {
        }

        /// <summary>
        /// 是否能够修改类型
        /// </summary>
        public bool CanEditType => OldField == null;

        /// <summary>
        /// 是否导出
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 旧字段
        /// </summary>
        public FieldInfo OldField { get; set; }
    }
}