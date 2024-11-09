using FzLib;
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
        public bool IsCorrectType(object propertyValue)
        {
            switch (Type)
            {
                case FieldInfoType.Integer:
                    return propertyValue is int;

                case FieldInfoType.Float:
                    return propertyValue is double;

                case FieldInfoType.Date:
                    return propertyValue is DateTime || propertyValue is DateTimeOffset || propertyValue is DateOnly;
                case FieldInfoType.Text:
                    return propertyValue is string;

                case FieldInfoType.DateTime:
                    return propertyValue is DateTime;

                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}