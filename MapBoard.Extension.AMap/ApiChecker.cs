using Newtonsoft.Json.Linq;

namespace MapBoard.Extension.AMap
{
    public static class ApiChecker
    {
        public static void CheckResponse(JObject root)
        {
            if (root["infocode"] is JValue && root["infocode"].Value<string>() != "10000")
            {
                throw new ApiException(root["info"].Value<string>());
            }
            if(root["errcode"] is JValue && root["errcode"].Value<int>()!=0)
            {
                throw new ApiException(root["errdetail"].Value<string>());
            }
        }
    }
}