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
                if (a.Key != Parameters.ClassFieldName
                    && a.Key != Parameters.LabelFieldName
                    && a.Key != Parameters.DateFieldName
                    && a.Key != Parameters.CreateTimeFieldName
                    && a.Key != "FID")
                {
                    result.Add(a.Key, a.Value);
                }
            }
            return result;
        }
    }
}