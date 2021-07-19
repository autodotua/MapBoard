using Esri.ArcGISRuntime;
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

namespace MapBoard.Mapping.Model
{
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

        public MapLayerInfo Find(FeatureLayer layer)
        {
            return LayerList.Cast<MapLayerInfo>().FirstOrDefault(p => p.Layer == layer);
        }

        public IMapLayerInfo Selected
        {
            get => selected;
            set
            {
                if (value != null)
                {
                    SelectedIndex = IndexOf(value);
                }
                else
                {
                    SelectedIndex = -1;
                }
                this.SetValueAndNotify(ref selected, value, nameof(Selected));
            }
        }

        public static async Task<MapLayerCollection> GetInstanceAsync(ELayerCollection esriLayers)
        {
            string path = Path.Combine(Parameters.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                return new MapLayerCollection(esriLayers);
            }
            MapLayerCollection instance = null;

            var tempLayers = FromFile(path);
            instance = new MapLayerCollection(esriLayers);
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MLayerCollection, MapLayerCollection>();
            }).CreateMapper().Map(tempLayers, instance);
            foreach (var layer in tempLayers)
            {
                await instance.AddAsync(layer);
            }
            List<string> errorMsgs = new List<string>();

            if (instance.SelectedIndex >= 0
                && instance.SelectedIndex < instance.Count)
            {
                instance.Selected = instance[instance.SelectedIndex] as MapLayerInfo;
            }
            return instance;
        }

        public async Task LoadAsync(ELayerCollection esriLayers)
        {
            EsriLayers = esriLayers;
            SetLayers(new ObservableCollection<ILayerInfo>());
            string path = Path.Combine(Parameters.DataPath, LayersFileName);
            if (!File.Exists(path))
            {
                return;
            }
            var tempLayers = FromFile(path);
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MLayerCollection, MapLayerCollection>();
            }).CreateMapper().Map(tempLayers, this);
            foreach (var layer in tempLayers)
            {
                await AddAsync(layer);
            }
            List<string> errorMsgs = new List<string>();

            if (SelectedIndex >= 0
                && SelectedIndex < Count)
            {
                Selected = this[SelectedIndex] as MapLayerInfo;
            }
        }

        public async Task<ILayerInfo> AddAsync(ILayerInfo layer)
        {
            if (!(layer is MapLayerInfo))
            {
                switch (layer.Type)
                {
                    case null:
                    case "":
                    case MapLayerInfo.Types.Shapefile:
                        layer = new ShapefileMapLayerInfo(layer);
                        break;

                    case MapLayerInfo.Types.WFS:
                        layer = new WfsMapLayerInfo(layer);
                        break;
                }
            }
            await AddLayerAsync(layer as MapLayerInfo, 0);
            (layer as MapLayerInfo).PropertyChanged += OnLayerPropertyChanged;
            LayerList.Add(layer);
            return layer;
        }

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
            EsriLayers.Insert(index, layer.Layer);
        }

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

        public async Task InsertAsync(int index, MapLayerInfo layer)
        {
            await AddLayerAsync(layer, Count - index);
            layer.PropertyChanged += OnLayerPropertyChanged;
            LayerList.Insert(index, layer);
        }

        public void Move(int fromIndex, int toIndex)
        {
            EsriLayers.Move(Count - fromIndex - 1, Count - toIndex - 1);
            LayerList.Move(fromIndex, toIndex);
        }

        public void Remove(MapLayerInfo layer)
        {
            try
            {
                EsriLayers.Remove(layer.Layer);
                layer.Dispose();
            }
            catch
            {
            }
            layer.PropertyChanged -= OnLayerPropertyChanged;
            LayerList.Remove(layer);
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        public void Save()
        {
            Save(Path.Combine(Parameters.DataPath, LayersFileName));
        }

        public ELayerCollection EsriLayers { get; private set; }

        private async Task AddLayerAsync(MapLayerInfo layer, int index)
        {
            try
            {
                if (!layer.HasTable)
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
                FeatureLayer fl = layer.Layer;
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
    }
}