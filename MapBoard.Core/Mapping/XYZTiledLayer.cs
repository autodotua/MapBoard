using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using FzLib.Collection;
using MapBoard.IO;
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

    public class XYZTiledLayer : ImageTiledLayer
    {
        private readonly static ConcurrentDictionary<string, byte[]> cacheQueueFiles = new ConcurrentDictionary<string, byte[]>();

        private readonly static ConcurrentQueue<string> cacheWriterQuque = new ConcurrentQueue<string>();

        private static HttpClient client;

        static XYZTiledLayer()
        {
            Task.Run(async () =>
            {
                //有两个集合，Queue用来确定任务的顺序，然后用Dictionary来获取任务数据。
                //同时Dictionary也可以用来检测是否已经提交过相同的任务，防止重复提交
                while (true)
                {
                    try {
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
                    catch(Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    await Task.Delay(1000);
                }
            });
        }
        private XYZTiledLayer(string url, string userAgent, Esri.ArcGISRuntime.ArcGISServices.TileInfo tileInfo, Envelope fullExtent, bool enableCache) : base(tileInfo, fullExtent)
        {
            Url = url;
            EnableCache = enableCache;
            id = BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "");
            if (client == null)
            {
                var socketsHttpHandler = new SocketsHttpHandler()
                {
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                };
                client = new HttpClient(socketsHttpHandler);
                if (!string.IsNullOrEmpty(userAgent))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                }

                //client.SendAsync(new HttpRequestMessage
                //{
                //    Method = new HttpMethod("HEAD"),
                //    RequestUri = new Uri(Url)
                //}).ConfigureAwait(false);
            }
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
        /// 瓦片地址的ID
        /// </summary>
        private readonly string id;

        public static XYZTiledLayer Create(string url, string userAgent, bool enableCache = false)
        {
            var webTiledLayer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
            return new XYZTiledLayer(url, userAgent, webTiledLayer.TileInfo, webTiledLayer.FullExtent, enableCache);
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
            if (EnableCache && File.Exists(cacheFile))
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
