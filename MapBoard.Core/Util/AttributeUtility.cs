using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using static MapBoard.Mapping.Model.FeaturesChangedSource;
using MapBoard.Mapping.Model;
using Esri.ArcGISRuntime.Data;
using System.Diagnostics;
using System.Linq;

namespace MapBoard.Util
{
    public static class AttributeUtility
    {
        public static async Task<ItemsOperationErrorCollection> CopyAttributesAsync(IEditableLayerInfo layer, FieldInfo fieldSource, FieldInfo fieldTarget, string dateFormat)
        {
            var features = await layer.GetAllFeaturesAsync();
            return await CopyAttributesAsync(layer, features, fieldSource, fieldTarget, dateFormat);
        }

        /// <summary>
        /// 将一个字段的属性赋值给另一个字段
        /// </summary>
        /// <param name="layer">操作的图层</param>
        /// <param name="fieldSource">作为模板的字段</param>
        /// <param name="fieldTarget">需要更改的字段</param>
        /// <param name="dateFormat">日期格式</param>
        /// <returns></returns>
        public static async Task<ItemsOperationErrorCollection> CopyAttributesAsync(IEditableLayerInfo layer, IEnumerable<Feature> features, FieldInfo fieldSource, FieldInfo fieldTarget, string dateFormat)
        {
            if (fieldTarget.Name.Equals(FieldExtension.CreateTimeField.Name))
            {
                throw new ArgumentException("不可为“创建时间”字段赋值");
            }

            Debug.Assert(features.All(p => p.FeatureTable.Layer == layer.Layer));
            ItemsOperationErrorCollection errors = new ItemsOperationErrorCollection();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();
            await Task.Run(() =>
            {
                foreach (var feature in features)
                {
                    newFeatures.Add(new UpdatedFeature(feature, feature.Geometry, new Dictionary<string, object>(feature.Attributes)));
                    object oldValue = feature.GetAttributeValue(fieldTarget.Name);
                    object value = feature.GetAttributeValue(fieldSource.Name);
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
                                //小数转整数
                                case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Float:
                                    result = Convert.ToInt32(value);
                                    break;
                                //文本转整数
                                case FieldInfoType.Integer when fieldSource.Type == FieldInfoType.Text:
                                    if (int.TryParse(value as string, out int i))
                                    {
                                        result = i;
                                    }
                                    else
                                    {
                                        errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{value}", "无法转为整数"));
                                    }
                                    break;
                                //整数转小数
                                case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Integer:
                                    result = Convert.ToDouble(value);

                                    break;
                                //文本转小数
                                case FieldInfoType.Float when fieldSource.Type == FieldInfoType.Text:
                                    if (double.TryParse(value as string, out double d))
                                    {
                                        result = d;
                                    }
                                    else
                                    {
                                        errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{value}", "无法转为小数"));
                                    }
                                    break;
                                //文本转日期
                                case FieldInfoType.Date when fieldSource.Type == FieldInfoType.Text:
                                    if (DateTime.TryParseExact(value as string, dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt))
                                    {
                                        result = dt;
                                    }
                                    else
                                    {
                                        errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{value}", "无法转为日期"));
                                    }
                                    break;
                                //文本转时间
                                case FieldInfoType.Time when fieldSource.Type == FieldInfoType.Text:
                                    result = value;
                                    break;
                                //日期转文本
                                case FieldInfoType.Text when fieldSource.Type == FieldInfoType.Date:
                                    result = ((DateTime)value).Date.ToString(dateFormat);
                                    break;
                                //任意转文本
                                case FieldInfoType.Text:
                                    result = value.ToString();
                                    break;

                                default:
                                    throw new NotSupportedException($"不支持的转换类型：从{fieldSource.Type}到{fieldTarget.Type}");
                            }
                            feature.SetAttributeValue(fieldTarget.Name, result);
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{value}", ex));
                        }
                    }
                }
            });
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
            return errors;
        }

        public static async Task<ItemsOperationErrorCollection> SetAttributesAsync(IEditableLayerInfo layer, FieldInfo field, string text, bool includeField, string dateFormat)
        {
            var features = await layer.GetAllFeaturesAsync();
            return await SetAttributesAsync(layer, features, field, text, includeField, dateFormat);
        }

        /// <summary>
        ///为要素批量设置属性
        /// </summary>
        /// <param name="layer">操作的图层</param>
        /// <param name="features">所需要修改的要素</param>
        /// <param name="field">需要更改的字段</param>
        /// <param name="text">目标字符串</param>
        /// <param name="includeField">是否包含其它字段，字段包含在[]中</param>
        /// <param name="dateFormat">日期格式</param>
        /// <returns></returns>
        public static async Task<ItemsOperationErrorCollection> SetAttributesAsync(IEditableLayerInfo layer, IEnumerable<Feature> features, FieldInfo field, string text, bool includeField, string dateFormat)
        {
            if (field.Name.Equals(FieldExtension.CreateTimeField.Name))
            {
                throw new ArgumentException("不可为“创建时间”字段赋值");
            }

            Debug.Assert(features.All(p => p.FeatureTable.Layer == layer.Layer));

            ItemsOperationErrorCollection errors = new ItemsOperationErrorCollection();
            List<UpdatedFeature> newFeatures = new List<UpdatedFeature>();
            await Task.Run(() =>
            {
                //查询需要替换的字段
                List<FieldInfo> sourceFields = includeField ?
                layer.Fields.Where(p => text.Contains($"[{p.Name}]")).ToList() : new List<FieldInfo>();

                foreach (var feature in features)
                {
                    newFeatures.Add(new UpdatedFeature(feature, feature.Geometry, new Dictionary<string, object>(feature.Attributes)));
                    object oldValue = feature.GetAttributeValue(field.Name);
                    string textValue = text;
                    //进行字段替换
                    if (includeField && sourceFields.Count > 0)
                    {
                        foreach (var f in sourceFields)
                        {
                            if (f.Type == FieldInfoType.Date)
                            {
                                if (feature.GetAttributeValue(f.Name) is DateTimeOffset dto)
                                {
                                    textValue = textValue.Replace($"[{f.Name}]", dto.DateTime.ToString(dateFormat));
                                }
                                else
                                {
                                    textValue = "";
                                }
                            }
                            else
                            {
                                textValue = textValue.Replace($"[{f.Name}]"
                                  , feature.GetAttributeValue(f.Name)?.ToString() ?? "");
                            }
                        }
                    }
                    object result = null;
                    try
                    {
                        switch (field.Type)
                        {
                            case FieldInfoType.Integer:
                                if (int.TryParse(textValue, out int i))
                                {
                                    result = i;
                                }
                                else
                                {
                                    errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{result}", "无法转为整数"));
                                }
                                break;

                            case FieldInfoType.Float:
                                if (double.TryParse(textValue, out double d))
                                {
                                    result = d;
                                }
                                else
                                {
                                    errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{result}", "无法转为小数"));
                                }
                                break;

                            case FieldInfoType.Date:
                                if (DateTime.TryParseExact(textValue, dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt))
                                {
                                    result = dt;
                                }
                                else
                                {
                                    errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{result}", "无法转为日期"));
                                }
                                break;

                            case FieldInfoType.Time:
                            case FieldInfoType.Text:
                                result = textValue;
                                break;

                            default:
                                throw new NotSupportedException();
                        }

                        feature.SetAttributeValue(field.Name, result);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ItemsOperationError($"{feature.GetID()}: {oldValue}=>{result}", ex));
                    }
                }
            });
            await layer.UpdateFeaturesAsync(newFeatures, FeatureOperation);
            return errors;
        }
    }
}