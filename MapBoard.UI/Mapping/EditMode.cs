namespace MapBoard.Mapping
{
    /// <summary>
    /// 地图编辑模式
    /// </summary>
    public enum EditMode
    {
        /// <summary>
        /// 未处于编辑状态
        /// </summary>
        None,

        /// <summary>
        /// 正在创建新图形
        /// </summary>
        Create,

        /// <summary>
        /// 正在编辑已有图形
        /// </summary>
        Edit,

        /// <summary>
        /// 正在获取一个图形（比如用于分割等）
        /// </summary>
        GetGeometry,

        /// <summary>
        /// 正在测量长度
        /// </summary>
        MeasureLength,

        /// <summary>
        /// 正在测量面积
        /// </summary>
        MeasureArea
    }
}