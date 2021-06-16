namespace MapBoard.Extension
{
    public interface IReGeoCodeEngine : IExtensionEngine
    {
        string GetUrl(
   Location location,
   double radius);

        /// <summary>
        /// 根据范围的字符串，解析为<see cref="LocationInfo"/>
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        LocationInfo ParseLocationInfo(string json);
    }
}