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

        private DateTime? dateValue;

        private double? floatValue;

        private int? intValue;

        private string textValue;

        private DateTime? timeValue;

        public FeatureAttribute()
        {
        }

        /// <summary>
        /// 日期类型
        /// </summary>
        public static string DateFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>
        /// 事件类型
        /// </summary>
        public static string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        /// <summary>
        /// 日期型值
        /// </summary>
        [AlsoNotifyFor(nameof(Value))]
        public DateTime? DateValue
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
        public int? IntValue
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
        /// 日期时间型值
        /// </summary>
        [AlsoNotifyFor(nameof(Value))]
        public DateTime? TimeValue
        {
            get => timeValue;
            set
            {
                if (Type != FieldInfoType.Time)
                {
                    throw new NotSupportedException();
                }
                attrValue = value;
                timeValue = value;
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
                            if (value == null)
                            {
                                attrValue = null;
                                intValue = null;
                                break;
                            }
                            if (value is int)
                            {
                                attrValue = value;
                                intValue = (int)value;
                                break;
                            }
                            if (value is long l)
                            {
                                if (l <= int.MaxValue && l >= int.MinValue)
                                {
                                    intValue = Convert.ToInt32(l);
                                    attrValue = intValue;
                                    break;
                                }
                                throw new ArgumentOutOfRangeException("输入的值超过了范围");
                            }
                            if (value is double d)
                            {
                                if (d <= int.MaxValue && d >= int.MinValue)
                                {
                                    intValue = Convert.ToInt32(d);
                                    attrValue = intValue;
                                    break;
                                }
                                throw new ArgumentOutOfRangeException("输入的值超过了范围");
                            }
                            if (value is string str1)
                            {
                                if (int.TryParse(str1, out int result))
                                {
                                    intValue = result;
                                    attrValue = intValue;
                                    break;
                                }
                            }
                            throw new ArgumentException("输入的值无法转换为整数");
                        case FieldInfoType.Float:
                            if (value == null)
                            {
                                floatValue = null;
                                attrValue = floatValue;
                                break;
                            }
                            if (value is double)
                            {
                                floatValue = (double)value;
                                attrValue = floatValue;
                                break;
                            }
                            if (value is string str2)
                            {
                                if (double.TryParse(str2, out double result))
                                {
                                    floatValue = result;
                                    attrValue = floatValue;
                                    break;
                                }
                            }
                            throw new ArgumentException("输入的值无法转换为小数");
                        case FieldInfoType.Date:
                            if (value == null)
                            {
                                dateValue = null;
                                attrValue = dateValue;
                                break;
                            }
                            if (value is DateTime dt)
                            {
                                dateValue = dt;
                                attrValue = dateValue;
                                break;
                            }
                            if (value is DateTimeOffset dto)
                            {
                                dateValue = dto.DateTime;
                                attrValue = dateValue;
                                break;
                            }
                            if (value is string str3)
                            {
                                DateTime result;
                                if (DateTime.TryParse(str3, out result))
                                {
                                    dateValue = result;
                                    attrValue = dateValue;
                                    break;
                                }
                                if (DateTime.TryParseExact(str3, DateTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                                {
                                    dateValue = result;
                                    attrValue = dateValue;
                                    break;
                                }
                            }
                            throw new ArgumentException("输入的值无法转换为日期");
                        case FieldInfoType.Time:
                            if (value == null)
                            {
                                timeValue = null;
                                attrValue = timeValue;
                                break;
                            }
                            if (value is DateTime t)
                            {
                                timeValue = t;
                                attrValue = timeValue;
                                break;
                            }
                            if (value is DateTimeOffset to)
                            {
                                timeValue = to.DateTime;
                                attrValue = timeValue;
                                break;
                            }
                            if (value is string str5)
                            {
                                if (DateTime.TryParse(str5, out DateTime result))
                                {
                                    timeValue = result;
                                    attrValue = timeValue;
                                    break;
                                }
                            }
                            throw new ArgumentException("输入的值无法转换为时间");
                        case FieldInfoType.Text:
                            if (value is string str4)
                            {
                                if (str4.Length > 254)
                                {
                                    throw new ArgumentException("输入的字符串过长");
                                }
                                textValue = str4;
                                attrValue = value;
                                break;
                            }
                            else if (value is null)
                            {
                                attrValue = textValue = null;
                            }
                            else
                            {
                                attrValue = textValue = value.ToString();
                            }
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
    }
}