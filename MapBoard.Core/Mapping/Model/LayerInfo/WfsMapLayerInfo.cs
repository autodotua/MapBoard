using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using FzLib;
using FzLib.Collection;
using MapBoard.Model;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.Mapping.Model
{
    //http://192.168.1.18:8080/geoserver/topp/ows?service=WFS&request=GetCapabilities
    public class WfsMapLayerInfo : MapLayerInfo, IServerBasedLayer
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
            if (layer.ServiceParameters.ContainsKey(nameof(AutoPopulateAll))
                && (layer.ServiceParameters[nameof(AutoPopulateAll)] == true.ToString()
                || layer.ServiceParameters[nameof(AutoPopulateAll)] == false.ToString()))
            {
                AutoPopulateAll = bool.Parse(layer.ServiceParameters[nameof(AutoPopulateAll)]);
            }
            Fields = null;
        }

        public WfsMapLayerInfo(string name, string url, string layerName, bool autoPopulateAll) : base(name)
        {
            SetService(url, layerName, autoPopulateAll);
            Fields = null;
        }

#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行

        public override async Task ChangeNameAsync(string newName, Esri.ArcGISRuntime.Mapping.LayerCollection layers)
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
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

        [JsonIgnore]
        public bool HasPopulateAll { get; private set; }

        [JsonIgnore]
        public bool IsDownloading { get; set; }

        public async Task PopulateAllFromServiceAsync(CancellationToken? cancellationToken = null)
        {
            HasPopulateAll = true;
            IsDownloading = true;
            await PopulateFromServiceAsync(new QueryParameters(), false, cancellationToken);
            IsDownloading = false;
        }

        public async Task<FeatureQueryResult> PopulateFromServiceAsync(QueryParameters parameters, bool clearCache = false, CancellationToken? cancellationToken = null)
        {
            FeatureQueryResult result = null;
            IsDownloading = true;
            if (cancellationToken == null)
            {
                result = await (table as WfsFeatureTable).PopulateFromServiceAsync(parameters, clearCache, null);
            }
            else
            {
                result = await (table as WfsFeatureTable).PopulateFromServiceAsync(parameters, clearCache, null, cancellationToken.Value);
            }
            IsDownloading = false;
            this.Notify(nameof(NumberOfFeatures));
            return result;
        }

        public override string Type => Types.WFS;

        [JsonIgnore]
        public string Url { get; private set; }

        [JsonIgnore]
        public string LayerName { get; private set; }

        [JsonIgnore]
        public bool AutoPopulateAll { get; private set; }

        public void SetService(string url, string layerName, bool autoPopulateAll)
        {
            Url = url;
            LayerName = layerName;
            AutoPopulateAll = autoPopulateAll;
            ServiceParameters.AddOrSetValue(nameof(Url), Url);
            ServiceParameters.AddOrSetValue(nameof(LayerName), LayerName);
            ServiceParameters.AddOrSetValue(nameof(AutoPopulateAll), AutoPopulateAll.ToString());
        }

        protected override async void ApplyProperties()
        {
            if (AutoPopulateAll)
            {
                await PopulateAllFromServiceAsync();
            }
        }
    }
}