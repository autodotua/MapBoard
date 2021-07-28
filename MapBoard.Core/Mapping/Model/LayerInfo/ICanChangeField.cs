using MapBoard.Model;
using System.Collections.Generic;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 表示能够修改字段的图层
    /// </summary>
    public interface ICanChangeField
    {
        void SetField(IEnumerable<FieldInfo> fields);
    }
}