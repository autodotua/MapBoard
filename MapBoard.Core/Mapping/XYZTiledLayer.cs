using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{

    public class XYZTiledLayer : ImageTiledLayer
    {
        public static XYZTiledLayer Create(string url,string userAgent)
        {
            var webTiledLayer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
            return new XYZTiledLayer(url,userAgent, webTiledLayer.TileInfo, webTiledLayer.FullExtent);
        }
        private XYZTiledLayer(string url,string userAgent, Esri.ArcGISRuntime.ArcGISServices.TileInfo tileInfo, Envelope fullExtent) : base(tileInfo, fullExtent)
        {
            Url = url;
            client = new HttpClient();
            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }
            //client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(UserAgentName, UserAgentVersion));
        }

        private  readonly HttpClient client;

        public string Url { get; }

        protected override async Task<ImageTileData> GetTileDataAsync(int level, int row, int column, CancellationToken cancellationToken)
        {
            string url = Url.Replace("{x}", column.ToString()).Replace("{y}", row.ToString()).Replace("{z}", level.ToString());
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            using var content = response.EnsureSuccessStatusCode().Content;
            return new ImageTileData(level, row, column, await content.ReadAsByteArrayAsync().ConfigureAwait(false), "");
        }

    }

}
