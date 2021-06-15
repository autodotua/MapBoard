namespace MapBoard.Extension
{
    public interface IRouteEngine : IExtensionEngine
    {
        string GetUrl(
   RouteType type,
   Location origin,
   Location destination);

        /// <summary>
        /// 根据范围的字符串，解析为<see cref="RouteInfo"/>
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        RouteInfo[] ParseRoute(RouteType type, string json);
    }
}