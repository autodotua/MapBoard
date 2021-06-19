namespace MapBoard.Model
{
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