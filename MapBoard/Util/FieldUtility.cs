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

        public static async Task CopyAttributesAsync(LayerInfo layer, FieldInfo fieldSource, FieldInfo fieldTarget, string dateFormat)
        {
            var features = await layer.GetAllFeaturesAsync();
            foreach (var feature in features)
            {
                object value = feature.Attributes[fieldSource.Name];
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
                                result = ((DateTimeOffset)value).Date.ToString(dateFormat);
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
                {
                new FieldInfo(Resource.LabelFieldName, "标签", FieldInfoType.Text),
                new FieldInfo(Resource.DateFieldName, "日期", FieldInfoType.Date),
                new FieldInfo(Resource.ClassFieldName, "分类", FieldInfoType.Text),
                };
        }

        public static IEnumerable<FieldInfo> IncludeDefaultFields(this IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                yield return field;
            }
            if (!fields.Any(p => p.Name == Resource.LabelFieldName))
            {
                yield return new FieldInfo(Resource.LabelFieldName, "标签", FieldInfoType.Text);
            }
            if (!fields.Any(p => p.Name == Resource.DateFieldName))
            {
                yield return new FieldInfo(Resource.DateFieldName, "日期", FieldInfoType.Date);
            }
            if (!fields.Any(p => p.Name == Resource.ClassFieldName))
            {
                yield return new FieldInfo(Resource.ClassFieldName, "分类", FieldInfoType.Text);
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
    }
}