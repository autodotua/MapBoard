using FzLib;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MapBoard.Model
{
    [DebuggerDisplay("Name={Name} Disp={DisplayName} Type={Type}")]
    public class FieldInfo : INotifyPropertyChanged, ICloneable
    {
        private string name = "";

        public FieldInfo(string name, string displayName, FieldInfoType type)
        {
            this.name = name;
            DisplayName = displayName;
            Type = type;
        }

        public FieldInfo()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName { get; set; } = "";

        public string Name
        {
            get => name;
            set
            {
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

        public FieldInfoType Type { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool IsCorrectType(object propertyValue)
        {
            switch (Type)
            {
                case FieldInfoType.Integer:
                    return propertyValue is int;

                case FieldInfoType.Float:
                    return propertyValue is double;

                case FieldInfoType.Date:
                    return propertyValue is DateTime || propertyValue is DateTimeOffset;

                case FieldInfoType.Text:
                    return propertyValue is string;

                case FieldInfoType.Time:
                    return propertyValue is string;

                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}