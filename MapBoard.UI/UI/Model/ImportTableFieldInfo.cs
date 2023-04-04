using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.UI.Model
{
    /// <summary>
    /// 导入要素表格中的字段信息
    /// </summary>
    public class ImportTableFieldInfo
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 字段
        /// </summary>
        public FieldInfo Field { get; } = new FieldInfo();

        /// <summary>
        /// 是否导入
        /// </summary>
        public bool Import { get; set; } = true;
    }
}