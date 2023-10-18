using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.IO;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapBoard.Mapping
{
    /// <summary>
    /// 带缓存的WebTiledLayer
    /// </summary>
    /// <remarks>
    /// XYZTiledLayer在MAUI上卡顿明显，因此重新基于WebTileLayer的父类、ImageTiledLayer的子类ServiceImageTiledLayer设计了一种支持缓存的瓦片图层。
    /// 重写GetTileUriAsync方法，若存在缓存，直接返回文件Uri，否则返回网址Uri，同时进行记录。
    /// 在静态类创建时创建一个定时器，不停循环，如果有需要缓存的瓦片，则重新进行下载并缓存。
    /// 这样，同时进行下载缓存的线程就只有1个，减少了资源占用。
    /// </remarks>
    public class CacheableWebTiledLayer : ServiceImageTiledLayer
    {
        private readonly static ConcurrentDictionary<string, string> cacheQueueFiles = new ConcurrentDictionary<string, string>();
        private readonly static ConcurrentQueue<string> cacheQueue = new ConcurrentQueue<string>();
        private static HttpClient client;
        private readonly string id = null;
        static CacheableWebTiledLayer()
        {
            var socketsHttpHandler = new SocketsHttpHandler()
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            };
            client = new HttpClient(socketsHttpHandler);
            client.Timeout = TimeSpan.FromSeconds(5);

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        while (!cacheQueue.IsEmpty && cacheQueue.TryDequeue(out string cacheUrl))
                        {
                            if (cacheQueueFiles.TryRemove(cacheUrl, out string cacheFile))
                            {
                                using var response = await client.GetAsync(cacheUrl);
                                using var content = response.EnsureSuccessStatusCode().Content;
                                var data = await content.ReadAsByteArrayAsync();
                                string dir = Path.GetDirectoryName(cacheFile);
                                if (!Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                File.WriteAllBytes(cacheFile, data);

                                Debug.WriteLine($"写入Tile缓存，queue={cacheQueue.Count}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    await Task.Delay(1000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public CacheableWebTiledLayer(string url, Esri.ArcGISRuntime.ArcGISServices.TileInfo tileInfo, Envelope fullExtent) : base(tileInfo, fullExtent)
        {
            id = BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "");
            Url = url;
        }

        public string Url { get; }
        public static CacheableWebTiledLayer Create(string url)
        {
            var webTiledLayer = new WebTiledLayer(url.Replace("{x}", "{col}").Replace("{y}", "{row}").Replace("{z}", "{level}"));
            return new CacheableWebTiledLayer(url, webTiledLayer.TileInfo, webTiledLayer.FullExtent);
        }
        protected async override Task<Uri> GetTileUriAsync(int level, int row, int column, CancellationToken cancellationToken)
        {
            Debug.WriteLine("Thread ID::::::::::::::::::" + Thread.CurrentThread.ManagedThreadId);

            string cacheFile = Path.Combine(FolderPaths.TileCachePath, id, level.ToString(), row.ToString(), column.ToString());
            if (File.Exists(cacheFile))//缓存优先
            {
                return new Uri(cacheFile, UriKind.Absolute);
            }
            else
            {
                string url = Url.Replace("{x}", column.ToString()).Replace("{y}", row.ToString()).Replace("{z}", level.ToString());
                cacheQueue.Enqueue(url);
                cacheQueueFiles.TryAdd(url, cacheFile);
                return new Uri(url);
            }
        }
    }

}
