﻿using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib;
using MapBoard.Model;
using MapBoard.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MLayerCollection = MapBoard.Model.LayerCollection;
using ELayerCollection = Esri.ArcGISRuntime.Mapping.LayerCollection;
using AutoMapper;
using MapBoard.IO;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 包含ArcGIS类型的图层集合
    /// </summary>
    public class MapLayerCollection : MLayerCollection
    {
        public const string LayersFileName = "layers.json";

        private IMapLayerInfo selected;

        public MapLayerCollection()
        {
        }

        private MapLayerCollection(ELayerCollection esriLayers)
        {
            EsriLayers = esriLayers;
            SetLayers(new ObservableCollection<ILayerInfo>());
        }

        /// <summary>
        /// 获取其中可编辑的图层
        /// </summary>
        public IEnumerable<MapLayerInfo> EditableLayers => this.OfType<MapLayerInfo>().Where(p => p.CanEdit);

        /// <summary>
        /// 对应的ArcGIS图层
        /// </summary>
        public ELayerCollection EsriLayers { get; private set; }

        /// <summary>
        /// 当前选中的图层
        /// </summary>
        public IMapLayerInfo Selected
        {
            get => selected;
            set
            {
                SelectedIndex = value != null ? IndexOf(value) : -1;
                selected = value;
            }
        }

        /// <summary>
        /// 从本地加载配置文件，生成图层
        /// </summary>
        /// <param name="esriLayers"></param>
        /// <returns></returns>
        public static async Task<MapLayerCollection> GetInstanceAsync(ELayerCollection esriLayers)
        {
            string path = Path.Combine(FolderPaths.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                return new MapLayerCollection(esriLayers);
            }
            MapLayerCollection instance = null;

            //读取到临时对象
            var tempLayers = FromFile(path);
            instance = new MapLayerCollection(esriLayers);
            //将临时变量映射到新的对象中
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MLayerCollection, MapLayerCollection>();
            }).CreateMapper().Map(tempLayers, instance);
            //将临时变量中的图层添加到新的MapLayerCollection对象中
            foreach (var layer in tempLayers)
            {
                await instance.AddAsync(layer);
            }
            //如果选定了某个图层，则将其设置为选定图层
            if (instance.SelectedIndex >= 0
                && instance.SelectedIndex < instance.Count)
            {
                instance.Selected = instance[instance.SelectedIndex] as MapLayerInfo;
            }
            return instance;
        }

        /// <summary>
        /// 根据图层信息，创建新图层并插入到最后
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task<ILayerInfo> AddAsync(ILayerInfo layer)
        {
            if (layer is not MapLayerInfo)
            {
                layer = layer.Type switch
                {
                    MapLayerInfo.Types.Shapefile => new ShapefileMapLayerInfo(layer),
                    MapLayerInfo.Types.WFS => new WfsMapLayerInfo(layer),
                    MapLayerInfo.Types.Temp => new TempMapLayerInfo(layer),
                    null => new ShapefileMapLayerInfo(layer),
                    _ => throw new NotSupportedException("不支持的图层格式：" + layer.Type)
                };
            }
            await AddLayerAsync(layer as MapLayerInfo, 0);
            (layer as MapLayerInfo).PropertyChanged += OnLayerPropertyChanged;
            LayerList.Add(layer);
            return layer;
        }

        /// <summary>
        /// 清空所有图层
        /// </summary>
        public void Clear()
        {
            foreach (var layer in EsriLayers.ToArray())
            {
                EsriLayers.Remove(layer);
            }
            foreach (MapLayerInfo layer in LayerList)
            {
                layer.Dispose();
            }
            LayerList.Clear();
        }

        /// <summary>
        /// 根据ArcGIS的图层，找到对应的<see cref="MapLayerInfo"/>
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public MapLayerInfo Find(FeatureLayer layer)
        {
            return LayerList.Cast<MapLayerInfo>().FirstOrDefault(p => p.Layer == layer);
        }

        /// <summary>
        /// 获取加载时遇到的错误
        /// </summary>
        /// <returns></returns>
        public ItemsOperationErrorCollection GetLoadErrors()
        {
            var c = new ItemsOperationErrorCollection();
            foreach (var layer in LayerList.Cast<MapLayerInfo>().Where(p => p.LoadError != null))
            {
                c.Add(new ItemsOperationError(layer.Name, layer.LoadError));
            }
            return c.Count == 0 ? null : c;
        }

        /// <summary>
        /// 插入一个图层到最后
        /// </summary>
        /// <param name="index"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public async Task InsertAsync(int index, MapLayerInfo layer)
        {
            await AddLayerAsync(layer, Count - index);
            layer.PropertyChanged += OnLayerPropertyChanged;
            LayerList.Insert(index, layer);
        }

        /// <summary>
        /// 加载所有图层
        /// </summary>
        /// <param name="esriLayers"></param>
        /// <returns></returns>
        public async Task LoadAsync(ELayerCollection esriLayers)
        {
            EsriLayers = esriLayers;
            //初始化一个新的图层列表
            SetLayers(new ObservableCollection<ILayerInfo>());
            //图层配置文件
            string path = Path.Combine(FolderPaths.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                return;
            }
            //获取临时图层对象，并映射到当前对象。使用临时对象是因为无法将对象反序列化后直接应用到当前对象，对象类型可能不一致，属性可能被覆盖。
            var tempLayers = FromFile(path);
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MLayerCollection, MapLayerCollection>();
            }).CreateMapper().Map(tempLayers, this);
            //将临时对象中所有图层加入当前对象
            foreach (var layer in tempLayers)
            {
                await AddAsync(layer);
            }
            //如果选定了某个图层，则将其设置为选定图层
            if (SelectedIndex >= 0
                && SelectedIndex < Count)
            {
                Selected = this[SelectedIndex] as MapLayerInfo;
            }
        }

        /// <summary>
        /// 交换两个图层的位置
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        public void Move(int fromIndex, int toIndex)
        {
            EsriLayers.Move(Count - fromIndex - 1, Count - toIndex - 1);
            LayerList.Move(fromIndex, toIndex);
        }

        /// <summary>
        /// 在更新Esri图层后，进行重新插入动作以刷新画面
        /// </summary>
        /// <param name="layer"></param>
        public void RefreshEsriLayer(IMapLayerInfo layer)
        {
            if (IndexOf(layer) < 0)
            {
                throw new ArgumentException("图层不在图层集合中");
            }
            int index = Count - 1 - IndexOf(layer);
            EsriLayers.RemoveAt(index);
            EsriLayers.Insert(index, layer.GetLayerForLayerList());
        }
        public void Remove(MapLayerInfo layer)
        {
            try
            {
                EsriLayers.Remove(layer.GetLayerForLayerList());
                layer.Dispose();
            }
            catch
            {
            }
            layer.PropertyChanged -= OnLayerPropertyChanged;
            LayerList.Remove(layer);
        }

        /// <summary>
        /// 保存图层配置
        /// </summary>
        public void Save()
        {
            Save(Path.Combine(FolderPaths.DataPath, LayersFileName));
        }

        /// <summary>
        /// 插入图层到指定位置
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task AddLayerAsync(IMapLayerInfo layer, int index)
        {
            try
            {
                //加载图层
                if (!layer.IsLoaded)
                {
                    try
                    {
                        await layer.LoadAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"加载图层{layer.Name}失败：{ex.Message}");
                    }
                }
                
                //添加ArcGIS图层到ArcGIS图层列表
                Layer fl = layer.GetLayerForLayerList();
                Debug.Assert(fl != null);
                if (index == -1)
                {
                    EsriLayers.Add(fl);
                }
                else
                {
                    EsriLayers.Insert(index, fl);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    layer.Dispose();
                    if (layer.Layer != null)
                    {
                        EsriLayers.Remove(layer.Layer);
                    }
                }
                catch
                {
                }
                throw;
            }
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
        }
    }
}