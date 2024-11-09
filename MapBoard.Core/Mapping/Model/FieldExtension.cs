using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.Model;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 字段扩展方法类
    /// </summary>
    public static class FieldExtension
    {
        /// <summary>
        /// 创建时间字段
        /// </summary>
        public static FieldInfo CreateTimeField => new FieldInfo(Parameters.CreateTimeFieldName, "创建时间", FieldInfoType.DateTime);

        /// <summary>
        /// 修改时间字段
        /// </summary>
        public static FieldInfo ModifiedTimeField => new FieldInfo(Parameters.ModifiedTimeFieldName, "修改时间", FieldInfoType.DateTime);

        /// <summary>
        /// 是否为能够作为符号系统Key的字段
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool CanBeRendererKey(this FieldInfo field)
        {
            return field.Type is FieldInfoType.Text or FieldInfoType.Integer or FieldInfoType.Float;
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
                if (!field.IsIdField())
                {
                    result.Add(field.Name, field.ToFieldInfo());
                }
            }
            return result;
        }

        /// <summary>
        /// 获取各类型字段的默认长度
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static int GetLength(this FieldInfoType type)
        {
            return type switch
            {
                FieldInfoType.Integer => 9,
                FieldInfoType.Float => 13,
                FieldInfoType.Date => 9,
                FieldInfoType.Text => 254,
                FieldInfoType.DateTime => 20,
                _ => throw new ArgumentException(),
            };
        }

        /// <summary>
        /// 是否存在某个类型、某个名称的字段
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasField(this ILayerInfo layer, string name, FieldInfoType type)
        {
            return layer.Fields.Any(p => p.Name == name && p.Type == type);
        }

        /// <summary>
        /// 是否为ID字段
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsIdField(this Field field)
        {
            return IsIdField(field.Name);
        }

        /// <summary>
        /// 是否为ID字段
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsIdField(this FieldInfo field)
        {
            return IsIdField(field.Name);
        }

        /// <summary>
        /// 是否为ID字段名
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsIdField(string field)
        {
            return field.Equals("fid", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("objectid", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将字段信息与数据源进行同步
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IEnumerable<FieldInfo> SyncWithSource(FieldInfo[] fields, FeatureTable table)
        {
            foreach (var esriField in table.Fields.Where(p => !p.IsIdField()))
            {
                var oldField = fields.FirstOrDefault(p => p.Name == esriField.Name);
                var field = esriField.ToFieldInfo();
                if (oldField != null)
                {
                    field.DisplayName = oldField.DisplayName;
                }
                yield return field;
            }
        }

        /// <summary>
        /// 转为ArcGIS的字段类型
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
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
                    type = FieldType.DateOnly;
                    length = 9;
                    break;

                case FieldInfoType.Text:
                    type = FieldType.Text;
                    length = 254;
                    break;

                case FieldInfoType.DateTime:
                    type = FieldType.Date;
                    //yyyy-MM-dd-HH-mm-ss
                    length = 20;
                    break;

                default:
                    break;
            }
            return new Field(type, field.Name, null, length);
        }

        /// <summary>
        /// 转为ArcGIS的字段类型
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static FieldDescription ToFieldDescription(this FieldInfo field)
        {
            FieldType type = default;
            switch (field.Type)
            {
                case FieldInfoType.Integer:
                    type = FieldType.Int64;
                    break;

                case FieldInfoType.Float:
                    type = FieldType.Float64;
                    break;

                case FieldInfoType.Date:
                    type = FieldType.DateOnly;
                    break;

                case FieldInfoType.Text:
                    type = FieldType.Text;
                    break;

                case FieldInfoType.DateTime:
                    type = FieldType.Date;
                    break;

                default:
                    break;
            }
            return new FieldDescription(field.Name, type);
        }

        /// <summary>
        /// 转为ArcGIS的字段类型
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static IEnumerable<Field> ToEsriFields(this IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                yield return field.ToEsriField();
            }
        }
        /// <summary>
        /// 从ArcGIS的字段类型转为应用字段类型
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static FieldInfo ToFieldInfo(this Field field)
        {
            var type = (int)field.FieldType switch
            {
                (int)FieldType.OID or (int)FieldType.Int16 or (int)FieldType.Int32 or (int)FieldType.Int64 => FieldInfoType.Integer,
                (int)FieldType.Float32 or (int)FieldType.Float64 => FieldInfoType.Float,
                (int)FieldType.Date => FieldInfoType.DateTime,
                (int)FieldType.Text => FieldInfoType.Text,
                (int)FieldType.DateOnly => FieldInfoType.Date,
                _ => throw new NotSupportedException(),
            };
            string name = field.Name;

            //对不符合要求的字段名进行转换
            if (string.IsNullOrEmpty(name)
                || name.Length > 10
              || !Regex.IsMatch(name[0].ToString(), "[a-zA-Z]")
                  || !Regex.IsMatch(name, "^[a-zA-Z0-9_]+$"))
            {
                name = $"f_{Math.Abs(field.Name.GetHashCode())}".Substring(0, 10);
            }
            string alias = string.IsNullOrEmpty(field.Alias) ? field.Name : field.Alias;
            return new FieldInfo(name, alias, type);
        }
    }
}