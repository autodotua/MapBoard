using Newtonsoft.Json.Linq;

namespace MapBoard.Extension.AMap
{
    internal static class JsonHelper
    {
        public static T TryGetValue<T>(this JToken jtoken, string key)
        {
            if (jtoken == null)
            {
                return default;
            }
            if (jtoken[key] is JValue value)
            {
                return value.Value<T>();
            }
            return default;
        }

        public static T TryGetValue<T>(this JToken jtoken, string key, T defaultValue)
        {
            if (jtoken == null)
            {
                return defaultValue;
            }
            if (jtoken[key] is JValue value)
            {
                return value.Value<T>();
            }
            return defaultValue;
        }

        public static Location ToLocation(this string str)
        {
            if (str == null)
            {
                return null;
            }
            var parts = str.Split(',');
            if (parts.Length != 2)
            {
                throw new ApiException("坐标格式不正确");
            }
            return new Location(double.Parse(parts[0]), double.Parse(parts[1]));
        }
    }
}