using Esri.ArcGISRuntime.Data;
using FzLib.Basic.Collection;
using FzLib.Extension;
using MapBoard.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    //http://192.168.1.18:8080/geoserver/topp/ows?service=WFS&request=GetCapabilities
    public class WfsMapLayerInfo : MapLayerInfo
    {
        public WfsMapLayerInfo(ILayerInfo layer) : base(layer)
        {
            if (layer.ServiceParameters.ContainsKey(nameof(Url)))
            {
                Url = layer.ServiceParameters[nameof(Url)];
            }
            if (layer.ServiceParameters.ContainsKey(nameof(LayerName)))
            {
                LayerName = layer.ServiceParameters[nameof(LayerName)];
            }
            Fields = null;
        }

        public WfsMapLayerInfo(string name, string url, string layerName) : base(name)
        {
            SetService(url, layerName);
            Fields = null;
        }

        public async override Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
        {
            Name = newName;
        }

        protected override FeatureTable GetTable()
        {
            var table = new WfsFeatureTable(new Uri(Url), LayerName) { FeatureRequestMode = FeatureRequestMode.ManualCache };
            table.Loaded += Table_Loaded;
            return table;
        }

        private void Table_Loaded(object sender, EventArgs e)
        {
            Fields = table.Fields.FromEsriFields().Values.ToArray();
        }

        public async Task<FeatureQueryResult> PopulateFromServiceAsync(QueryParameters parameters, bool clearCache = false, CancellationToken? cancellationToken = null)
        {
            FeatureQueryResult result = null;
            if (cancellationToken == null)
            {
                result = await (table as WfsFeatureTable).PopulateFromServiceAsync(parameters, clearCache, null);
            }
            else
            {
                result = await (table as WfsFeatureTable).PopulateFromServiceAsync(parameters, clearCache, null, cancellationToken.Value);
            }
            this.Notify(nameof(NumberOfFeatures));
            return result;
        }

        public override string Type => Types.WFS;
        public override bool IsEditable => false;

        public string Url { get; private set; }
        public string LayerName { get; private set; }

        public void SetService(string url, string layerName)
        {
            Url = url;
            LayerName = layerName;
            ServiceParameters.AddOrSetValue(nameof(Url), Url);
            ServiceParameters.AddOrSetValue(nameof(LayerName), LayerName);
        }
    }
}