using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Collection;
using MapBoard.IO;
using MapBoard.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    /// <summary>
    /// XYZ瓦片图层，WebTiledLayer的更灵活的实现
    /// </summary>
    public class XYZTiledLayer : ImageTiledLayer
    {
        private readonly static ConcurrentDictionary<string, byte[]> cacheQueueFiles = new ConcurrentDictionary<string, byte[]>();

        private readonly static ConcurrentQueue<string> cacheWriterQuque = new ConcurrentQueue<string>();

        /// <summary>
        /// 瓦片地址的ID
        /// </summary>
        private readonly string id;

        private HttpClient client;

        static XYZTiledLayer()
        {
            Task.Run(async () =>
            {
                //单队列写入缓存
                //有两个集合，Queue用来确定任务的顺序，然后用Dictionary来获取任务数据。
                //同时Dictionary也可以用来检测是否已经提交过相同的任务，防止重复提交
                while (true)
                {
                    try
                    {
                        while (!cacheWriterQuque.IsEmpty && cacheWriterQuque.TryDequeue(out string cacheFile))
                        {
                            if (cacheQueueFiles.TryGetValue(cacheFile, out byte[] data))
                            {
                                string dir = Path.GetDirectoryName(cacheFile);
                                if (!Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                File.WriteAllBytes(cacheFile, data);
                                cacheQueueFiles.TryRemove(cacheFile, out _);
                                Debug.WriteLine($"写入Tile缓存，queue={cacheWriterQuque.Count}, hashset={cacheQueueFiles.Count}");
                            }
                            else
                            {
                                Debug.WriteLine("待写入的缓存文件不在Dictionary中");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    await Task.Delay(1000);
                }
            });
        }
        private XYZTiledLayer(BaseLayerInfo layerInfo, string userAgent, Esri.ArcGISRuntime.ArcGISServices.TileInfo tileInfo, Envelope fullExtent, bool enableCache) : base(tileInfo, fullExtent)
        {
            Url = layerInfo.Path;
            EnableCache = enableCache;
            id = BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(layerInfo.Path))).Replace("-", "");

            var socketsHttpHandler = new SocketsHttpHandler()
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            };
            client = new HttpClient(socketsHttpHandler);
            ApplyHttpClientHeaders(client,layerInfo,userAgent);

           
            //client.DefaultRequestHeaders.Add("Host", "t3.tianditu.gov.cn");
            //client.DefaultRequestHeaders.Add("Origin", "https://zhejiang.tianditu.gov.cn");
            //client.DefaultRequestHeaders.Add("Referer", "https://zhejiang.tianditu.gov.cn");

            //client.SendAsync(new HttpRequestMessage
            //{
            //    Method = new HttpMethod("HEAD"),
            //    RequestUri = new Uri(Url)
            //}).ConfigureAwait(false);
            //client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(UserAgentName, UserAgentVersion));
        }

        /// <summary>
        /// 是否启用缓存机制
        /// </summary>
        public bool EnableCache { get; }

        /// <summary>
        /// 瓦片地址的模板链接
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// 应用Http客户端的请求头
        /// </summary>
        /// <param name="client"></param>
        /// <param name="layerInfo"></param>
        /// <param name="defaultUserAgent"></param>
        public static void ApplyHttpClientHeaders(HttpClient client, BaseLayerInfo layerInfo, string defaultUserAgent)
        {
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            if (!string.IsNullOrWhiteSpace(layerInfo.UserAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", layerInfo.UserAgent);
            }
            else if (!string.IsNullOrEmpty(defaultUserAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", defaultUserAgent);
            }
            if (!string.IsNullOrWhiteSpace(layerInfo.Host))
            {
                client.DefaultRequestHeaders.Add("Host", layerInfo.Host);
            }
            if (!string.IsNullOrWhiteSpace(layerInfo.Origin))
            {
                client.DefaultRequestHeaders.Add("Origin", layerInfo.Origin);
            }
            if (!string.IsNullOrWhiteSpace(layerInfo.Referer))
            {
                client.DefaultRequestHeaders.Add("Referer", layerInfo.Referer);
            }
            if (!string.IsNullOrWhiteSpace(layerInfo.OtherHeaders))
            {
                foreach (var headerAndValue in layerInfo.OtherHeaders.Split('|').Select(p => p.Split(':')))
                {
                    if (headerAndValue.Length >= 2)
                    {
                        client.DefaultRequestHeaders.Add(headerAndValue[0], headerAndValue[1]);
                    }
                }
            }
        }

        /// <summary>
        /// 创建图层
        /// </summary>
        /// <param name="layerInfo"></param>
        /// <param name="userAgent"></param>
        /// <param name="enableCache"></param>
        /// <returns></returns>
        public static XYZTiledLayer Create(BaseLayerInfo layerInfo, string userAgent, bool enableCache = false)
        {
            string url = layerInfo.Path;
            var webTiledLayer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
            return new XYZTiledLayer(layerInfo, userAgent, webTiledLayer.TileInfo, webTiledLayer.FullExtent, enableCache);
        }

        /// <summary>
        /// 创建图层
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userAgent"></param>
        /// <param name="enableCache"></param>
        /// <returns></returns>
        public static XYZTiledLayer Create(string url, string userAgent, bool enableCache = false)
        {
            return Create(new BaseLayerInfo(BaseLayerType.WebTiledLayer, url), userAgent, enableCache);
        }

        /// <summary>
        /// 获取指定的瓦片图层
        /// </summary>
        /// <param name="level"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<ImageTileData> GetTileDataAsync(int level, int row, int column, CancellationToken cancellationToken)
        {
            string cacheFile = Path.Combine(FolderPaths.TileCachePath, id, level.ToString(), row.ToString(), column.ToString());
            byte[] data = null;
            if (EnableCache && File.Exists(cacheFile))//缓存优先
            {
                data = await File.ReadAllBytesAsync(cacheFile);
            }
            else
            {
                string url = Url.Replace("{x}", column.ToString()).Replace("{y}", row.ToString()).Replace("{z}", level.ToString());
                using var response = await client.GetAsync(url, cancellationToken);
                using var content = response.EnsureSuccessStatusCode().Content;
                data = await content.ReadAsByteArrayAsync(cancellationToken);
                if (EnableCache && !File.Exists(cacheFile) && !cacheQueueFiles.ContainsKey(cacheFile))
                {
                    cacheQueueFiles.TryAdd(cacheFile, data);
                    cacheWriterQuque.Enqueue(cacheFile);
                }
            }
            return new ImageTileData(level, row, column, data, "");
        }

    }

}
