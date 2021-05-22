using MapBoard.Common;
using System.Collections.Generic;

namespace MapBoard.Main.Model.Extension
{
    public static class FeatureAttributeExtension
    {
        public static Dictionary<string, object> GetCustomAttributes(this IDictionary<string, object> attributes)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var a in attributes)
            {
                if (a.Key != Resource.ClassFieldName
                    && a.Key != Resource.LabelFieldName
                    && a.Key != Resource.DateFieldName
                    && a.Key != "FID")
                {
                    result.Add(a.Key, a.Value);
                }
            }
            return result;
        }
    }
}