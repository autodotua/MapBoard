using FzLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MapBoard.Model
{
    /// <summary>
    /// 图层集合
    /// </summary>
    public class LayerCollection : IReadOnlyList<ILayerInfo>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public LayerCollection()
        {
        }

        /// <summary>
        /// 图层集合改变事件，将和LayerList的同步
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler<LayerPropertyChangedEventArgs> LayerPropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 图层数量
        /// </summary>
        public int Count => LayerList.Count;

        /// <summary>
        /// 地图边界位置
        /// </summary>
        public string MapViewExtentJson { get; set; }

        /// <summary>
        /// 当前选择的图层序号
        /// </summary>
        public int SelectedIndex { get; set; }

        /// <summary>
        /// 实际的图层列表
        /// </summary>
        protected ObservableCollection<ILayerInfo> LayerList { get; private set; }

        public ILayerInfo this[int index] => LayerList[index];

        /// <summary>
        /// 从序列化后的JSON文件加载图层
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static LayerCollection FromFile(string path)
        {
            var instance = new LayerCollection();

            JObject json = JObject.Parse(File.ReadAllText(path));
            instance.MapViewExtentJson = json[nameof(MapViewExtentJson)]?.Value<string>();
            instance.SelectedIndex = json[nameof(SelectedIndex)]?.Value<int>() ?? 0;
            var layersJson = json["Layers"];
            instance.SetLayers(new ObservableCollection<ILayerInfo>());
            if (layersJson is JArray layerJsonArray)
            {
                foreach (JObject jLayer in layerJsonArray)
                {
                    var layer = jLayer.ToObject<LayerInfo>();
                    //用于迁移旧版的Symbols到新版的Renderer
                    VersionTransition.V20220222_SymbolsToRenderer(jLayer, layer);
                    instance.LayerList.Add(layer);
                }
            }
            return instance;
        }

        /// <summary>
        /// 是否包含某图层
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public bool Contains(ILayerInfo layer)
        {
            return LayerList.Contains(layer);
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ILayerInfo> GetEnumerator()
        {
            return LayerList?.GetEnumerator();
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return LayerList?.GetEnumerator();
        }

        public int IndexOf(ILayerInfo layer)
        {
            return LayerList?.IndexOf(layer) ?? -1;
        }

        /// <summary>
        /// 序列化为JSON
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            JObject json = new JObject
            {
                { nameof(MapViewExtentJson), MapViewExtentJson },
                { nameof(SelectedIndex), SelectedIndex },
                { "Layers", JArray.FromObject(LayerList) }
            };
            string jsonStr = json.ToString(Formatting.Indented);
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, jsonStr);
        }

        /// <summary>
        /// 设置数据源
        /// </summary>
        /// <param name="layers"></param>
        protected void SetLayers(ObservableCollection<ILayerInfo> layers)
        {
            Debug.Assert(layers != null);
            if (LayerList != null)
            {
                LayerList.CollectionChanged -= LayerList_CollectionChanged;
            }
            LayerList = layers;

            LayerList.CollectionChanged += LayerList_CollectionChanged;
        }

        /// <summary>
        /// 任意图层中某个属性发生改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LayerPropertyChanged?.Invoke(this, new LayerPropertyChangedEventArgs(sender as LayerInfo, e.PropertyName));
        }


        private void LayerList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IEnumerable<LayerInfo> newLayers = null;
            IEnumerable<LayerInfo> oldLayers = null;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    newLayers = e.NewItems.Cast<LayerInfo>();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    oldLayers = e.OldItems.Cast<LayerInfo>();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    oldLayers = e.OldItems.Cast<LayerInfo>();
                    newLayers = e.NewItems.Cast<LayerInfo>();
                    break;
            }
            if (newLayers != null)
            {
                newLayers.ForEach(p => p.PropertyChanged += Layer_PropertyChanged);
            }
            if (oldLayers != null)
            {
                oldLayers.ForEach(p => p.PropertyChanged -= Layer_PropertyChanged);
            }
            CollectionChanged?.Invoke(sender, e);
        }

        public class LayerPropertyChangedEventArgs : PropertyChangedEventArgs
        {
            public LayerPropertyChangedEventArgs(ILayerInfo layer, string propertyName) : base(propertyName)
            {
                Layer = layer;
            }

            public ILayerInfo Layer { get; set; }
        }
    }
}