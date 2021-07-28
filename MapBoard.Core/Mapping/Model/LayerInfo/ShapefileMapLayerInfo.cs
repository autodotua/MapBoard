using AutoMapper;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.IO;
using MapBoard.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public class ShapefileMapLayerInfo : EditableLayerInfo, IHasDefaultFields, IFileBasedLayer
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

        public override object Clone()
        {
            var layer = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<LayerInfo, ShapefileMapLayerInfo>();
            }).CreateMapper().Map<ShapefileMapLayerInfo>(this);
            return layer;
        }

        public override string Type => Types.Shapefile;

        protected override FeatureTable GetTable()
        {
            return new ShapefileFeatureTable(GetMainFilePath());
        }

        public override void Dispose()
        {
            (table as ShapefileFeatureTable)?.Close();
            table = null;
            base.Dispose();
        }

        public async override Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
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
            foreach (var file in Shapefile.GetExistShapefiles(Parameters.DataPath, Layer.Name))
            {
                if (File.Exists(Path.Combine(Parameters.DataPath, newName + Path.GetExtension(file))))
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
            foreach (var file in Shapefile.GetExistShapefiles(Parameters.DataPath, Layer.Name))
            {
                File.Move(file, Path.Combine(Parameters.DataPath, newName + Path.GetExtension(file)));
            }
            Name = newName;
            await LoadAsync();
            layers[index] = Layer;
        }

        public string[] GetFilePaths()
        {
            return Shapefile.GetExistShapefiles(Parameters.DataPath, Name).ToArray();
        }

        public string GetMainFilePath()
        {
            return Path.Combine(Parameters.DataPath, Name + ".shp");
        }

        public Task SaveTo(string directory)
        {
            return Shapefile.CloneFeatureToNewShpAsync(directory, this);
        }
    }
}