using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public static class FieldExtension
    {
        //public static IEnumerable<FieldInfo> IncludeDefaultFields(this IEnumerable<FieldInfo> fields)
        //{
        //    if (!fields.Any(p => p.Name == Parameters.CreateTimeFieldName))
        //    {
        //        yield return CreateTimeField;
        //    }
        //    if (!fields.Any(p => p.Name == Parameters.ModifiedTimeFieldName))
        //    {
        //        yield return ModifiedTimeField;
        //    }
        //    foreach (var field in fields)
        //    {
        //        yield return field;
        //    }
        //}

        public static bool IsIdField(this Field field)
        {
            return field.Name.ToLower() == "fid";
            //暂时不考虑GID、ID、ObjectID
        }

        public static bool IsIdField(this FieldInfo field)
        {
            return field.Name.ToLower() == "fid";
            //暂时不考虑GID、ID、ObjectID
        }

        public static IEnumerable<FieldInfo> SyncWithSource(FieldInfo[] fields, FeatureTable table)
        {
            foreach (var esriField in table.Fields.Where(p => !p.IsIdField()))
            {
                var oldField = fields.FirstOrDefault(p => p.Name == esriField.Name);
                var field = esriField.ToFieldInfo();
                if (oldField != null)
                {
                    field.DisplayName = oldField.DisplayName;
                    if (field.Type == FieldInfoType.Text && oldField.Type == FieldInfoType.Time)
                    {
                        field.Type = FieldInfoType.Time;
                    }
                }
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

        /// <summary>
        /// 从ArcGISRuntime到FiledInfo
        /// </summary>
        /// <param name="fields"></param>
        /// <returns>从原表字段名到新字段的映射</returns>
        public static Dictionary<string, FieldInfo> FromEsriFields(this IEnumerable<Field> fields)
        {
            Dictionary<string, FieldInfo> result = new Dictionary<string, FieldInfo>();
            foreach (var field in fields)
            {
                string name = field.Name.ToLower();
                if (name == "id" || name == "fid")
                {
                    continue;
                }
                result.Add(field.Name, field.ToFieldInfo());
            }
            return result;
        }

        public static FieldInfo ToFieldInfo(this Field field)
        {
            var type = (int)field.FieldType switch
            {
                (int)FieldType.OID or (int)FieldType.Int16 or (int)FieldType.Int32 or 2 => FieldInfoType.Integer,
                (int)FieldType.Float32 or (int)FieldType.Float64 => FieldInfoType.Float,
                (int)FieldType.Date => FieldInfoType.Date,
                (int)FieldType.Text => FieldInfoType.Text,
                _ => throw new NotSupportedException(),
            };
            string name = field.Name;
            //对不符合要求的字段名进行转换
            if (!Regex.IsMatch(name[0].ToString(), "[a-zA-Z]")
                  || !Regex.IsMatch(name, "^[a-zA-Z0-9_]+$"))
            {
                name = $"f_{Math.Abs(field.Name.GetHashCode())}".Substring(0, 10);
            }
            return new FieldInfo(name, field.Name, type);
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

        public static FieldInfo[] DefaultFields => new[] { CreateTimeField, ModifiedTimeField };
        public static FieldInfo CreateTimeField => new FieldInfo(Parameters.CreateTimeFieldName, "创建时间", FieldInfoType.Time);
        public static FieldInfo ModifiedTimeField => new FieldInfo(Parameters.ModifiedTimeFieldName, "修改时间", FieldInfoType.Time);
    }
}