using FzLib.Extension;
using System;

namespace MapBoard.Main.Model
{
    public class FeatureAttribute : FieldInfo
    {
        private object attrValue;

        public FeatureAttribute(FieldInfo field, object value = null)
        {
            Name = field.Name;
            DisplayName = field.DisplayName;
            Type = field.Type;
            Value = value;
        }

        public FeatureAttribute()
        {
        }

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
                                throw new ApplicationException("输入的值超过了范围");
                            }
                            if (value is double d)
                            {
                                if (d <= int.MaxValue && d >= int.MinValue)
                                {
                                    intValue = Convert.ToInt32(d);
                                    attrValue = intValue;
                                    break;
                                }
                                throw new ApplicationException("输入的值超过了范围");
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
                            throw new ApplicationException("输入的值无法转换为整数");
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
                            throw new ApplicationException("输入的值无法转换为小数");
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
                                if (DateTime.TryParse(str3, out DateTime result))
                                {
                                    dateValue = result;
                                    attrValue = dateValue;
                                    break;
                                }
                            }
                            throw new ApplicationException("输入的值无法转换为日期");
                        case FieldInfoType.Text:
                            if (value is string str4)
                            {
                                if (str4.Length > 254)
                                {
                                    throw new ApplicationException("输入的字符串过长");
                                }
                                textValue = str4;
                                attrValue = value;
                                break;
                            }
                            throw new ApplicationException("输入的值无法转换为字符串");
                        default:
                            throw new NotSupportedException();
                    }
                }
                catch (Exception ex)
                {
#if !DEBUG
                    throw;
#endif
                }
                finally
                {
                    this.Notify(nameof(Value));
                }
            }
        }

        private int? intValue;

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

        private double? floatValue;

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
                this.Notify(nameof(FloatValue), nameof(Value));
            }
        }

        private string textValue;

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
                this.Notify(nameof(TextValue), nameof(Value));
            }
        }

        private DateTime? dateValue;

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
                this.Notify(nameof(DateValue), nameof(Value));
            }
        }
    }
}