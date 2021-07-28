using System.Collections.Generic;

namespace MapBoard.Mapping.Model
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
                    && a.Key != "FID"
                    && a.Key != "ObjectID")
                {
                    result.Add(a.Key, a.Value);
                }
            }
            return result;
        }
    }
}