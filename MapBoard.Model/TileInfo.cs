namespace MapBoard.Model
{
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

        public int Level { get; }
        public int X { get; }
        public int Y { get; }
    }
}