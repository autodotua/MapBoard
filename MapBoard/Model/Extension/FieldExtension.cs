using Esri.ArcGISRuntime.Data;
using MapBoard.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Model.Extension
{
    public static class FieldExtension
    {
        public static IEnumerable<FieldInfo> IncludeDefaultFields(this IEnumerable<FieldInfo> fields)
        {
            if (!fields.Any(p => p.Name == Resource.LabelFieldName))
            {
                yield return FieldInfo.LabelField;
            }
            if (!fields.Any(p => p.Name == Resource.DateFieldName))
            {
                yield return FieldInfo.DateField;
            }
            if (!fields.Any(p => p.Name == Resource.ClassFieldName))
            {
                yield return FieldInfo.ClassField;
            }
            foreach (var field in fields)
            {
                yield return field;
            }
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

        public static IEnumerable<FieldInfo> FromEsriFields(this IEnumerable<Field> fields)
        {
            foreach (var field in fields)
            {
                string name = field.Name.ToLower();
                if (name == "id" || name == "fid")
                {
                    continue;
                }
                yield return ToFieldInfo(field);
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

        public static int GetLength(this FieldInfoType type)
        {
            switch (type)
            {
                case FieldInfoType.Integer:
                    return 9;

                case FieldInfoType.Float:
                    return 13;

                case FieldInfoType.Date:
                    return 9;

                case FieldInfoType.Text:
                    return 254;

                default:
                    throw new ArgumentException();
            }
        }
    }
}