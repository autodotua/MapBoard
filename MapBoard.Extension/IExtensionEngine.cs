namespace MapBoard.Extension
{
    public interface IExtensionEngine
    {
        /// <summary>
        /// POI搜索引擎名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 是否接受和返回GCJ02坐标而不是WGS84坐标
        /// </summary>
        bool IsGcj02 { get; }
    }
}