using Esri.ArcGISRuntime.Data;
using MapBoard.Common;
using MapBoard.Main.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Main.Util
{
    public static class FieldUtility
    {
        public static async Task CopyAttributesAsync(LayerInfo layer, FieldInfo fieldSource, FieldInfo fieldTarget, string dateFormat)
        {
            var features = await layer.GetAllFeaturesAsync();
            foreach (var feature in features)
            {
                object value = feature.Attributes[fieldSource.Name];
                if (value is DateTimeOffset dto)
                {
                    value = dto.UtcDateTime;
                }
                if (fieldTarget.Type == fieldSource.Type)
                {
                    feature.SetAttributeValue(fieldTarget.Name, feature.GetAttributeValue(fieldSource.Name));
                }
                else
                {
                    object result = null;
                    try
                    {
                        switch (fieldTarget.Type)
                        {
                            case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Float:
                                result = Convert.ToInt32(value);
                                break;

                            case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Text:
                                result = int.Parse(value as string);
                                break;

                            case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Integer:
                                result = Convert.ToDouble(value);
                                break;

                            case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Text:
                                result = double.Parse(value as string);
                                break;

                            case FieldInfoType.Date when fieldSource.Type == FieldInfoType.Text:
                                result = DateTime.ParseExact(value as string, dateFormat, CultureInfo.CurrentCulture);
                                break;

                            case FieldInfoType.Text when fieldSource.Type == FieldInfoType.Date:
                                result = ((DateTime)value).Date.ToString(dateFormat);
                                break;

                            case FieldInfoType.Text:
                                result = value.ToString();
                                break;
                        }
                    }
                    catch { }
                    feature.SetAttributeValue(fieldTarget.Name, result);
                }
                await layer.Table.UpdateFeatureAsync(feature);
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
                {             FieldInfo.LabelField,FieldInfo.DateField,FieldInfo.ClassField                };
        }

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

        public static IEnumerable<FieldInfo> ToFieldInfos(this IEnumerable<Field> fields)
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