using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.UI.Model
{
    public class ImportTableFieldInfo
    {
        public int ColumnIndex { get; set; }
        public bool Import { get; set; } = true;
        public FieldInfo Field { get; } = new FieldInfo();
        public string ColumnName { get; set; }
    }
}