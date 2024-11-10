#define ERROR

using FzLib;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace MapBoard.Model
{
    /// <summary>
    /// 单个要素属性
    /// </summary>
    public class FeatureAttribute : FieldInfo
    {
        private object attrValue;

        private DateTime? dateTimeValue;

        private DateOnly? dateValue;

        private double? floatValue;

        private long? intValue;

        private string textValue;
        public FeatureAttribute()
        {
        }

        /// <summary>
        /// 日期类型
        /// </summary>
        public static string DateFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>
        /// 日期时间类型
        /// </summary>
        public static string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";


        /// <summary>
        /// 日期时间型值
        /// </summary>
        [AlsoNotifyFor(nameof(Value))]
        public DateTime? DateTimeValue
        {
            get => dateTimeValue;
            set
            {
                if (Type != FieldInfoType.DateTime)
                {
                    throw new NotSupportedException();
                }
                attrValue = value;
                dateTimeValue = value;
            }
        }

        /// <summary>
        /// 日期型值
        /// </summary>
        [AlsoNotifyFor(nameof(Value))]
        public DateOnly? DateValue
        {
            get => dateValue;
            set
            {
                if (Type != FieldInfoType.Date)
                {
                    throw new NotSupportedException();
                }
                attrValue = value;
                dateValue = value;
            }
        }

        /// <summary>
        /// 浮点型值
        /// </summary>
        [AlsoNotifyFor(nameof(Value))]
        public double? FloatValue
        {
            get => floatValue;
            set
            {
                if (Type != FieldInfoType.Float)
                {
                    throw new NotSupportedException();
                }
                attrValue = value;
                floatValue = value;
            }
        }

        /// <summary>
        /// 整形值
        /// </summary>
        [AlsoNotifyFor(nameof(Value))]
        public long? IntValue
        {
            get => intValue;
            set
            {
                if (Type != FieldInfoType.Integer)
                {
                    throw new NotSupportedException();
                }
                attrValue = value;
                intValue = value;
                this.Notify(nameof(IntValue), nameof(Value));
            }
        }

        /// <summary>
        /// 文本型值
        /// </summary>
        [AlsoNotifyFor(nameof(Value))]
        public string TextValue
        {
            get => textValue;
            set
            {
                if (Type != FieldInfoType.Text)
                {
                    throw new NotSupportedException();
                }
                attrValue = value;
                textValue = value;
            }
        }

        /// <summary>
        /// 属性值
        /// </summary>
        public object Value
        {
            get => attrValue;
            set
            {
                try
                {
                    attrValue = Type switch
                    {
                        FieldInfoType.Integer => intValue = ParseToInt(value),
                        FieldInfoType.Float => floatValue = ParseToFloat(value),
                        FieldInfoType.Date => dateValue = ParseToDate(value),
                        FieldInfoType.DateTime => dateTimeValue = ParseToDateTime(value),
                        FieldInfoType.Text => textValue = ParseToText(value),
                        _ => throw new InvalidEnumArgumentException(),
                    };
                }
                catch (ArgumentException ex)
                {
#if !DEBUG||ERROR
                    throw;
#endif
                }
                finally
                {
                    this.Notify(nameof(Value));
                }
            }
        }

        /// <summary>
        /// 将一个属性值转换到另一个类型
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        public static object ConvertToType(object value, FieldInfoType targetType)
        {
            return targetType switch
            {
                FieldInfoType.Integer => ParseToInt(value),
                FieldInfoType.Float => ParseToFloat(value),
                FieldInfoType.Date => ParseToDate(value),
                FieldInfoType.DateTime => ParseToDateTime(value),
                FieldInfoType.Text => ParseToText(value),
                _ => throw new InvalidEnumArgumentException(),
            };
        }

        /// <summary>
        /// 根据ArcGIS字段和属性值，创建<see cref="FeatureAttribute"/>
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static FeatureAttribute FromFieldAndValue(FieldInfo field, object value = null)
        {
            FeatureAttribute attribute = new FeatureAttribute()
            {
                Name = field.Name,
                DisplayName = field.DisplayName,
                Type = field.Type,
            };
            try
            {
                attribute.Value = value;
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine(ex);
            }
            return attribute;
        }

        public static DateOnly? ParseToDate(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is DateOnly dd)
            {
                return dd;
            }
            else if (value is DateTime dt)
            {
                return DateOnly.FromDateTime(dt);
            }
            else if (value is DateTimeOffset dto)
            {
                return DateOnly.FromDateTime(dto.DateTime);
            }
            else if (value is string str)
            {
                if (DateOnly.TryParse(str, out DateOnly result))
                {
                    return result;
                }
                if (DateTime.TryParse(str, out DateTime dt2))
                {
                    return DateOnly.FromDateTime(dt2);
                }
                throw new ArgumentException("输入的字符串无法转换为日期");
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为日期");
            }
        }

        public static DateTime? ParseToDateTime(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is DateTime t)
            {
                return t;
            }
            else if (value is DateTimeOffset to)
            {
                return to.DateTime;
            }
            else if (value is string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return null;
                }
                if (DateTime.TryParse(str, out DateTime result))
                {
                    return result;
                }
                throw new ArgumentException("输入的字符串无法转换为时间");
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为时间");
            }
        }

        public static double? ParseToFloat(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is double or float)
            {
                return Convert.ToDouble(value);
            }
            else if (value is string str)
            {
                if (double.TryParse(str, out double result))
                {
                    return result;
                }
                throw new ArgumentException("输入的值无法转换为小数");
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为小数");
            }
        }

        public static long? ParseToInt(object value)
        {
            if (value == null)
            {
                return null;
            }
            else if (value is long or int or byte or short or sbyte or ushort or uint)
            {
                return Convert.ToInt64(value);
            }

            else if (value is double d)
            {
                if (d <= long.MaxValue && d >= long.MinValue)
                {
                    return Convert.ToInt64(d);
                }
                throw new ArgumentOutOfRangeException("输入的值超过了范围");
            }
            else if (value is string str)
            {
                if (long.TryParse(str, out long result))
                {
                    return result;
                }
                throw new ArgumentException("输入的字符串无法转换为整数");
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为整数");
            }
        }

        public static string ParseToText(object value)
        {
            if (value is string str)
            {
                return str;
            }
            else if (value is null)
            {
                return null;
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// 获取属性值的字符串表达
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Value == null)
            {
                return "";
            }
            if (Type == FieldInfoType.Date)
            {
                return DateValue.Value.ToShortDateString();
            }
            return Value.ToString();
        }
    }
}