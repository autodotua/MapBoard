using AutoMapper;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.IO;
using MapBoard.Model;
using MapBoard.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    /// <summary>
    /// 包含ArcGIS类型的Shapefile图层
    /// </summary>
    public class ShapefileMapLayerInfo : EditableLayerInfo, IFileBasedLayer
    {
        public ShapefileMapLayerInfo() : base()
        {
        }

        public ShapefileMapLayerInfo(ILayerInfo layer) : base(layer)
        {
        }

        public ShapefileMapLayerInfo(string name) : base(name)
        {
        }

        public ShapefileMapLayerInfo(MapLayerInfo template, string newName, bool includeFields)
        {
            //需要从模板对象中读取信息，写入到当前对象
            new MapperConfiguration(cfg =>
               {
                   cfg.CreateMap<LayerInfo, ShapefileMapLayerInfo>();
               }).CreateMapper().Map<LayerInfo, ShapefileMapLayerInfo>(template, this);
            Name = newName;

            if (!includeFields)
            {
                Fields = Array.Empty<FieldInfo>();
            }
        }

        public override string Type => Types.Shapefile;

        public override async Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            Debug.Assert(!string.IsNullOrEmpty(newName));
            if (newName == Name)
            {
                return;
            }
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                   || newName.Length > 240 || newName.Length < 1)
            {
                throw new IOException("新文件名不合法");
            }
            //检查文件存在
            foreach (var file in Shapefile.GetExistShapefiles(FolderPaths.DataPath, Layer.Name))
            {
                if (File.Exists(Path.Combine(FolderPaths.DataPath, newName + Path.GetExtension(file))))
                {
                    throw new IOException("该名称的文件已存在");
                }
            }
            //检查图层是否在集合中
            if (!layers.Contains(Layer))
            {
                throw new ArgumentException("本图层不在给定的图层集合中");
            }
            int index = layers.IndexOf(Layer);

            (table as ShapefileFeatureTable).Close();
            //重命名
            foreach (var file in Shapefile.GetExistShapefiles(FolderPaths.DataPath, Layer.Name))
            {
                File.Move(file, Path.Combine(FolderPaths.DataPath, newName + Path.GetExtension(file)));
            }
            Name = newName;
            await LoadAsync();
            layers[index] = Layer;
        }

        public override object Clone()
        {
            var layer = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<LayerInfo, ShapefileMapLayerInfo>();
            }).CreateMapper().Map<ShapefileMapLayerInfo>(this);
            return layer;
        }
        public override void Dispose()
        {
            (table as ShapefileFeatureTable)?.Close();
            table = null;
            base.Dispose();
        }

        public string[] GetFilePaths()
        {
            return Shapefile.GetExistShapefiles(FolderPaths.DataPath, Name).ToArray();
        }

        public string GetMainFilePath()
        {
            return Path.Combine(FolderPaths.DataPath, Name + ".shp");
        }

        /// <summary>
        /// 通过将要素复制到新建的Shapefile中的方式，修改字段
        /// </summary>
        /// <param name="newFields"></param>
        /// <param name="layers"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task ModifyFieldsAsync(FieldInfo[] newFields, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            //检查图层是否在集合中
            if (!layers.Contains(Layer))
            {
                throw new ArgumentException("本图层不在给定的图层集合中");
            }
            int index = layers.IndexOf(Layer);

            var oldFeatures = (await QueryFeaturesAsync(new QueryParameters())).ToList();
            (table as ShapefileFeatureTable).Close();
            //重命名
            await LayerUtility.DeleteLayerAsync(this, null, true);

            await LayerUtility.CreateShapefileLayerAsync(GeometryType, null, this, false, newFields, Name);

            await LoadAsync();
            layers[index] = Layer;
            try
            {
                await AddFeaturesAsync(oldFeatures, FeaturesChangedSource.Import);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 保存到新的位置
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public Task SaveTo(string directory)
        {
            return Shapefile.CloneFeatureToNewShpAsync(directory, this);
        }

        protected override FeatureTable GetTable()
        {
            return new ShapefileFeatureTable(GetMainFilePath());
        }

        public async Task UpdateExtent(Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            int index = layers.IndexOf(Layer);
            var extent = await QueryExtentAsync(new QueryParameters());
            (table as ShapefileFeatureTable).Close();
            await Shapefile.UpdateExtentAsync(GetMainFilePath(), extent);
            await LoadAsync();
            layers[index] = Layer;
        }
    }
}