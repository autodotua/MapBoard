namespace MapBoard.Model
{
    /// <summary>
    /// 字段赋值的类型
    /// </summary>
    public enum FieldAssignmentType
    {
        /// <summary>
        /// 从一个字段赋值给另一个字段
        /// </summary>
        Field = 0,

        /// <summary>
        /// 赋值为一个常量
        /// </summary>
        Const = 1,

        /// <summary>
        /// 更加灵活的赋值方式，自动替换包含在[]中的属性名为属性值
        /// </summary>
        Custom = 2
    }
}