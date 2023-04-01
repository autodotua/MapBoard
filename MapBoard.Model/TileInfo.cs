namespace MapBoard.Model
{
    /// <summary>
    /// 瓦片信息
    /// </summary>
    public class TileInfo
    {
        public TileInfo()
        {
        }

        public TileInfo(int level, int x, int y)
        {
            Level = level;
            X = x;
            Y = y;
        }

        /// <summary>
        /// 缩放等级
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// X编号
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Y编号
        /// </summary>
        public int Y { get; }
    }
}