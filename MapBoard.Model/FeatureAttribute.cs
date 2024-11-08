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
                    switch (Type)
                    {
                        case FieldInfoType.Integer:
                            ParseInt(value);
                            break;

                        case FieldInfoType.Float:
                            ParseFloat(value);
                            break;

                        case FieldInfoType.Date:
                            ParseDate(value);
                            break;

                        case FieldInfoType.DateTime:
                            ParseDateTime(value);
                            break;

                        case FieldInfoType.Text:
                            ParseText(value);
                            break;

                        default:
                            throw new InvalidEnumArgumentException();
                    }
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

        private void ParseDate(object value)
        {
            if (value == null)
            {
                dateValue = null;
                attrValue = dateValue;
            }
            else if (value is DateOnly dd)
            {
                dateValue = dd;
                attrValue = dateValue;
            }
            else if (value is DateTime dt)
            {
                dateValue = DateOnly.FromDateTime(dt);
                attrValue = dateValue;
            }
            else if (value is DateTimeOffset dto)
            {
                dateValue = DateOnly.FromDateTime(dto.DateTime);
                attrValue = dateValue;
            }
            else if (value is string str)
            {
                if (DateOnly.TryParse(str, out DateOnly result))
                {
                    dateValue = result;
                    attrValue = dateValue;
                }
                if (DateTime.TryParse(str, out DateTime dt2))
                {
                    dateValue = DateOnly.FromDateTime(dt2);
                    attrValue = dateValue;
                }
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为日期");
            }
        }

        private void ParseDateTime(object value)
        {
            if (value == null)
            {
                dateTimeValue = null;
                attrValue = dateTimeValue;
            }
            else if (value is DateTime t)
            {
                dateTimeValue = t;
                attrValue = dateTimeValue;
            }
            else if (value is DateTimeOffset to)
            {
                dateTimeValue = to.DateTime;
                attrValue = dateTimeValue;
            }
            else if (value is string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    attrValue = DateTimeValue = null;
                }
                else if (DateTime.TryParse(str, out DateTime result))
                {
                    dateTimeValue = result;
                    attrValue = dateTimeValue;
                }
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为时间");
            }
        }

        private void ParseFloat(object value)
        {
            if (value == null)
            {
                floatValue = null;
                attrValue = floatValue;
            }
            else if (value is double or float)
            {
                floatValue = Convert.ToDouble(value);
                attrValue = floatValue;
            }
            else if (value is string str)
            {
                if (double.TryParse(str, out double result))
                {
                    floatValue = result;
                    attrValue = floatValue;
                }
                throw new ArgumentException("输入的值无法转换为小数");
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为小数");
            }
        }

        private void ParseInt(object value)
        {
            if (value == null)
            {
                attrValue = null;
                intValue = null;
            }
            else if (value is long or int or byte or short or sbyte or ushort or uint)
            {
                intValue = Convert.ToInt64(value);
                attrValue = intValue;
            }

            else if (value is double d)
            {
                if (d <= long.MaxValue && d >= long.MinValue)
                {
                    intValue = Convert.ToInt64(d);
                    attrValue = intValue;
                }
                throw new ArgumentOutOfRangeException("输入的值超过了范围");
            }
            else if (value is string str)
            {
                if (long.TryParse(str, out long result))
                {
                    intValue = result;
                    attrValue = intValue;
                }
                throw new ArgumentException("输入的值无法转换为整数");
            }
            else
            {
                throw new ArgumentException("输入的值无法转换为整数");
            }
        }

        private void ParseText(object value)
        {
            if (value is string str)
            {
                textValue = str;
                attrValue = value;
            }
            else if (value is null)
            {
                attrValue = textValue = null;
            }
            else
            {
                attrValue = textValue = value.ToString();
            }
        }
    }
}