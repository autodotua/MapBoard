using Esri.ArcGISRuntime.Data;
using MapBoard.Common;
using MapBoard.Main.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Util
{
    public static class FieldUtility
    {
        public static IEnumerable<FieldInfo> GetCustomFields(this IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                if (field.Name != Resource.ClassFieldName
                    && field.Name != Resource.LabelFieldName
                    && field.Name != Resource.DateFieldName)
                {
                    yield return field;
                }
            }
        }

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

        public static FieldInfo[] GetDefaultFields()
        {
            return new FieldInfo[]
                {
                new FieldInfo("Label", "标签", FieldInfoType.Text),
                new FieldInfo("Date", "日期", FieldInfoType.Date),
                new FieldInfo("Class", "分类", FieldInfoType.Text),
                };
        }

        public static Field ToEsriField(this FieldInfo field)
        {
            FieldType type = default;
            int length = 0;
            switch (field.Type)
            {
                case FieldInfoType.Integer:
                    type = FieldType.Int32;
                    length = 9;
                    break;

                case FieldInfoType.Float:
                    type = FieldType.Float64;
                    length = 13;
                    break;

                case FieldInfoType.Date:
                    type = FieldType.Date;
                    length = 9;
                    break;

                case FieldInfoType.Text:
                    type = FieldType.Text;
                    length = 254;
                    break;

                default:
                    break;
            }
            return new Field(type, field.Name, null, length);
        }

        public static IEnumerable<Field> ToEsriFields(this IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                yield return ToEsriField(field);
            }
        }

        public static FieldInfo ToFieldInfo(this Field field)
        {
            FieldInfoType type;
            switch (field.FieldType)
            {
                case FieldType.OID:
                case FieldType.Int16:
                case FieldType.Int32:
                    type = FieldInfoType.Integer;
                    break;

                case FieldType.Float32:
                case FieldType.Float64:
                    type = FieldInfoType.Float;
                    break;

                case FieldType.Date:
                    type = FieldInfoType.Date;
                    break;

                case FieldType.Text:
                    type = FieldInfoType.Text;
                    break;

                default:
                    throw new NotSupportedException();
            }
            return new FieldInfo(field.Name, field.Name, type);
        }
    }
}