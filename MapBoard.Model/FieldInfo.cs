﻿using FzLib;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MapBoard.Model
{
    /// <summary>
    /// 字段信息
    /// </summary>
    [DebuggerDisplay("Name={Name} Disp={DisplayName} Type={Type}")]
    public class FieldInfo : INotifyPropertyChanged, ICloneable
    {
        private string name = "";

        public FieldInfo(string name, string displayName, FieldInfoType type)
        {
            Name = name;
            DisplayName = displayName;
            Type = type;
        }

        public FieldInfo()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 显示名（别名）
        /// </summary>
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// 字段名
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                //为了方便，只支持长度为1-10的英文大小写、数字和下划线组成的字段名
                if (string.IsNullOrWhiteSpace(value))
                {
                    name = "";
                }
                else if (value != null
                     && value.Length <= 10
                     && value.Length > 0
                     && Regex.IsMatch(value[0].ToString(), "[a-zA-Z]")
                     && Regex.IsMatch(value, "^[a-zA-Z0-9_]+$"))
                {
                    name = value;
                }
                else
                {
                }
            }
        }

        /// <summary>
        /// 字段类型
        /// </summary>
        public FieldInfoType Type { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// 判断属性值是否与字段类型对应
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        public bool IsCompatibleType(object propertyValue, out object value)
        {
            value = null;
            switch (Type)
            {
                case FieldInfoType.Integer:
                    if (propertyValue is long l)
                    {
                        value = l;
                        return true;
                    }
                    if (CanConvertToLong(propertyValue))
                    {
                        value = Convert.ToInt64(propertyValue);
                        return true;
                    }
                    return false;

                case FieldInfoType.Float:
                    if (propertyValue is double d)
                    {
                        value = d;
                        return true;
                    }
                    if (CanConvertToDouble(propertyValue))
                    {
                        value = Convert.ToDouble(propertyValue);
                        return true;
                    }
                    return propertyValue is double;

                case FieldInfoType.Date:
                    {
                        if (propertyValue is DateOnly date)
                        {
                            value = date;
                            return true;
                        }
                        if (propertyValue is DateTime dt)
                        {
                            value = DateOnly.FromDateTime(dt);
                            return true;
                        }
                        if (propertyValue is DateTimeOffset dto)
                        {
                            value = DateOnly.FromDateTime(dto.DateTime);
                            return true;
                        }
                        return false;
                    }
                case FieldInfoType.Text:
                    if (propertyValue is string str)
                    {
                        value = str;
                    }
                    else
                    {
                        value = propertyValue.ToString();
                    }
                    return true;

                case FieldInfoType.DateTime:
                    {
                        if (propertyValue is DateOnly date)
                        {
                            value = date.ToDateTime(TimeOnly.MinValue);
                            return true;
                        }
                        if (propertyValue is DateTime)
                        {
                            value = propertyValue;
                            return true;
                        }
                        if (propertyValue is DateTimeOffset dto)
                        {
                            value = dto.DateTime;
                            return true;
                        }
                        return false;
                    }
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        /// 判断属性值是否与字段类型对应
        /// </summary>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        public bool IsCorrectType(object propertyValue)
        {
            switch (Type)
            {
                case FieldInfoType.Integer:
                    return propertyValue is long;

                case FieldInfoType.Float:
                    return propertyValue is double;

                case FieldInfoType.Date:
                    return propertyValue is DateOnly;

                case FieldInfoType.Text:
                    return propertyValue is string;

                case FieldInfoType.DateTime:
                    return propertyValue is DateTime;

                default:
                    throw new InvalidEnumArgumentException();
            }
        }
        private static bool CanConvertToDouble(object o)
        {
            return System.Type.GetTypeCode(o.GetType()) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16
                or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16
                or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal
                or TypeCode.Double or TypeCode.Single => true,
                _ => false,
            };
        }

        private static bool CanConvertToLong(object o)
        {
            return System.Type.GetTypeCode(o.GetType()) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16
                or TypeCode.UInt32 or TypeCode.Int16
                or TypeCode.Int32 or TypeCode.Int64 => true,
                _ => false,
            };
        }
    }
}