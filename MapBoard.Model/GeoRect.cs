namespace MapBoard.Model
{
    /// <summary>
    /// 地理范围，可能为经纬度（<see cref="double"/>）、投影坐标单位（<see cref="double"/>）或瓦片位置（<see cref="int"/>）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GeoRect<T>
    {
        public GeoRect()
        {
        }

        public GeoRect(T xMinOrLeft, T xMaxOrRight, T yMinOrBottom, T yMaxOrTop)
        {
            SetValue(xMinOrLeft, xMaxOrRight, yMinOrBottom, yMaxOrTop);
        }

        public T XMax_Right { get; set; }
        public T XMin_Left { get; set; }
        public T YMax_Top { get; set; }
        public T YMin_Bottom { get; set; }

        public void SetValue(T xMinOrLeft, T xMaxOrRight, T yMinOrBottom, T yMaxOrTop)
        {
            XMin_Left = xMinOrLeft;
            XMax_Right = xMaxOrRight;
            YMin_Bottom = yMinOrBottom;
            YMax_Top = yMaxOrTop;
        }
    }
}