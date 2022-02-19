using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Collection;
using MapBoard.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    public class TempMapLayerInfo : EditableLayerInfo, ICanChangeField, ICanChangeGeometryType
    {
        private FeatureCollectionLayer featureCollectionLayer;

        public TempMapLayerInfo(ILayerInfo layer) : base(layer)
        {
        }

        public TempMapLayerInfo(string name, GeometryType type, IEnumerable<FieldInfo> fields) : base()
        {
            Set(name, type, fields);
        }

        private void Set(string name, GeometryType type, IEnumerable<FieldInfo> fields)
        {
            Name = name;
            ServiceParameters.AddOrSetValue(nameof(GeometryType), type.ToString());
            Fields = fields.ToArray();
        }

        public override async Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            this.Name = newName;
            await Task.Yield();
        }

        protected override FeatureTable GetTable()
        {
            GeometryType type = default;
            if (ServiceParameters.TryGetValue(nameof(GeometryType), out string typeStr)
                && Enum.TryParse(typeStr, out type))
            {
            }
            else
            {
                type = GeometryType.Polyline;
            }
            var fields = Fields?.ToEsriFields();
            FeatureCollectionTable table = new FeatureCollectionTable(fields, type, SpatialReferences.Wgs84);
            FeatureCollection collection = new FeatureCollection(new[] { table });
            featureCollectionLayer = new FeatureCollectionLayer(collection);
            return table;
        }

        public void SetGeometryType(GeometryType type)
        {
            ServiceParameters.AddOrSetValue(nameof(GeometryType), type.ToString());
        }

        public void SetField(IEnumerable<FieldInfo> fields)
        {
            Fields = fields.ToArray();
        }

        public override string Type => Types.Temp;

        public override Layer GetAddedLayer()
        {
            return featureCollectionLayer;
        }

        protected override FeatureLayer GetNewLayer(FeatureTable table)
        {
            return featureCollectionLayer.Layers[0];
        }
    }
}