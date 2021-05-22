﻿using FzLib.Extension;
using MapBoard.Common;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace MapBoard.Main.Model
{
    public enum FieldInfoType
    {
        [Description("整数")]
        Integer,

        [Description("小数")]
        Float,

        [Description("日期")]
        Date,

        [Description("文本")]
        Text
    }

    public class FieldInfo : INotifyPropertyChanged, ICloneable
    {
        private string displayName = "";
        private string name = "";

        private FieldInfoType type;

        public FieldInfo(string name, string displayName, FieldInfoType type)
        {
            Name = name;
            DisplayName = displayName;
            Type = type;
        }

        public FieldInfo()
        {
        }

        public static FieldInfo[] DefaultFields => new[] { LabelField, DateField, ClassField };
        public static readonly FieldInfo LabelField = new FieldInfo(Resource.LabelFieldName, "标签", FieldInfoType.Text);
        public static readonly FieldInfo DateField = new FieldInfo(Resource.DateFieldName, "日期", FieldInfoType.Date);
        public static readonly FieldInfo ClassField = new FieldInfo(Resource.ClassFieldName, "分类", FieldInfoType.Text);

        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName
        {
            get => displayName;
            set => this.SetValueAndNotify(ref displayName, value, nameof(DisplayName));
        }

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
                     && value.Length < 10
                     && value.Length > 0
                     && Regex.IsMatch(value[0].ToString(), "[a-zA-Z]")
                     && Regex.IsMatch(value, "^[a-zA-Z0-9_]+$"))
                {
                    name = value;
                }
                this.Notify(nameof(Name));
            }
        }

        public FieldInfoType Type
        {
            get => type;
            set => this.SetValueAndNotify(ref type, value, nameof(Type));
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}