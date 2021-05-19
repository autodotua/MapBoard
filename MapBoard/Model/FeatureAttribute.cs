using FzLib.Extension;
using System;
using System.ComponentModel;

namespace MapBoard.Main.Model
{
    public class FeatureAttribute : FieldInfo
    {
        private object attrValue;

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
                                break;
                            }
                            if (value is int)
                            {
                                attrValue = value;
                                break;
                            }
                            if (value is long l)
                            {
                                if (l <= int.MaxValue && l >= int.MinValue)
                                {
                                    attrValue = Convert.ToInt32(l);
                                    break;
                                }
                                throw new ApplicationException("输入的值超过了范围");
                            }
                            if (value is double d)
                            {
                                if (d <= int.MaxValue && d >= int.MinValue)
                                {
                                    attrValue = Convert.ToInt32(d);
                                    break;
                                }
                                throw new ApplicationException("输入的值超过了范围");
                            }
                            if (value is string str1)
                            {
                                if (int.TryParse(str1, out int result))
                                {
                                    attrValue = result;
                                    break;
                                }
                            }
                            throw new ApplicationException("输入的值无法转换为整数");
                        case FieldInfoType.Float:
                            if (value == null)
                            {
                                attrValue = null;
                                break;
                            }
                            if (value is double)
                            {
                                attrValue = value;
                                break;
                            }
                            if (value is string str2)
                            {
                                if (double.TryParse(str2, out double result))
                                {
                                    attrValue = result;
                                    break;
                                }
                            }
                            throw new ApplicationException("输入的值无法转换为小数");
                        case FieldInfoType.Date:
                            if (value == null)
                            {
                                attrValue = null;
                                break;
                            }
                            if (value is DateTime)
                            {
                                attrValue = value;
                                break;
                            }
                            if (value is string str3)
                            {
                                if (DateTime.TryParse(str3, out DateTime result))
                                {
                                    attrValue = result;
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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}