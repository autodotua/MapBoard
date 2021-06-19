using Esri.ArcGISRuntime.Data;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public static class FieldExtension
    {
        public static IEnumerable<FieldInfo> IncludeDefaultFields(this IEnumerable<FieldInfo> fields)
        {
            if (!fields.Any(p => p.Name == Parameters.LabelFieldName))
            {
                yield return LabelField;
            }
            if (!fields.Any(p => p.Name == Parameters.DateFieldName))
            {
                yield return DateField;
            }
            if (!fields.Any(p => p.Name == Parameters.ClassFieldName))
            {
                yield return ClassField;
            }
            if (!fields.Any(p => p.Name == Parameters.CreateTimeFieldName))
            {
                yield return CreateTimeField;
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

                case FieldInfoType.Time:
                    type = FieldType.Text;
                    //yyyy-MM-dd-HH-mm-ss
                    length = 20;
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
                yield return field.ToEsriField();
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
                yield return field.ToFieldInfo();
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
            string name = field.Name;
            return new FieldInfo(name, name, type);
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

                case FieldInfoType.Time:
                    return 20;

                default:
                    throw new ArgumentException();
            }
        }

        public static FieldInfo[] DefaultFields => new[] { LabelField, DateField, ClassField, CreateTimeField };
        public static readonly FieldInfo LabelField = new FieldInfo(Parameters.LabelFieldName, "标签", FieldInfoType.Text);
        public static readonly FieldInfo DateField = new FieldInfo(Parameters.DateFieldName, "日期", FieldInfoType.Date);
        public static readonly FieldInfo ClassField = new FieldInfo(Parameters.ClassFieldName, "分类", FieldInfoType.Text);
        public static readonly FieldInfo CreateTimeField = new FieldInfo(Parameters.CreateTimeFieldName, "创建时间", FieldInfoType.Time);
    }
}