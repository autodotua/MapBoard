using FzLib;
using FzLib.DataStorage.Serialization;
using MapBoard.IO;
using MapBoard.Model;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;

namespace MapBoard
{
    public class Config : IJsonSerializable, INotifyPropertyChanged
    {
        public static readonly int WatermarkHeight = 72;
        private static readonly string path = FolderPaths.ConfigPath;
        private static Config instance;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 配置类单例
        /// </summary>
        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Config();
                    try
                    {
                        instance.TryLoadFromJsonFile(path);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                return instance;
            }
        }

        public List<BaseLayerInfo> BaseLayers { get; set; } = new List<BaseLayerInfo>();
        public bool EnableBasemapCache { get; set; } = true;
        public bool IsTracking { get; set; }
        public int LastLayerListGroupType { get; set; }
        public double MaxScale { get; set; } = 100;
        public bool CanRotate { get; set; } = false;
        /// <summary>
        /// 保存配置到默认文件
        /// </summary>
        public void Save()
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            this.Save(path, new JsonSerializerSettings().SetIndented());
        }
    }
}