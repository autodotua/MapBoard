using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using MapBoard.IO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
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
    /// 带缓存的WebTiledLayer
    /// </summary>
    /// <remarks>
    /// XYZTiledLayer在MAUI上卡顿明显，因此重新基于WebTileLayer的父类、ImageTiledLayer的子类ServiceImageTiledLayer设计了一种支持缓存的瓦片图层。
    /// 重写GetTileUriAsync方法，若存在缓存，直接返回文件Uri，否则返回网址Uri，同时进行记录。
    /// 在静态类创建时创建一个定时器，不停循环，如果有需要缓存的瓦片，则重新进行下载并缓存。
    /// 这样，同时进行下载缓存的线程就只有1个，减少了资源占用。
    /// </remarks>
    public  class CacheableWebTiledLayer : ServiceImageTiledLayer
    {
        private readonly static ConcurrentQueue<TileCacheEntity> cacheQueue = new ConcurrentQueue<TileCacheEntity>();

        private static HttpClient httpClient;

        private static readonly TileCacheDbContext dbContext;

        private const string CacheFileDirName = "cacheFiles";

        private static readonly string CacheFileDir = Path.Combine(FolderPaths.CachePath, CacheFileDirName);

        static CacheableWebTiledLayer()
        {
            dbContext = new TileCacheDbContext();

            httpClient = new HttpClient(new SocketsHttpHandler() { PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5), })
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            if (Directory.Exists(CacheFileDir))
            {
                Directory.Delete(CacheFileDir, true);
            }
            Directory.CreateDirectory(CacheFileDir);

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        int count = 0;
                        while (!cacheQueue.IsEmpty && cacheQueue.TryDequeue(out TileCacheEntity tile))
                        {
                            byte[] bytes = await httpClient.GetByteArrayAsync(tile.TileUrl);
                            tile.Data = bytes;
                            dbContext.Tiles.Add(tile);
                            Debug.WriteLine($"写入Tile缓存，队列长度剩余{cacheQueue.Count}");
                            if (++count % 10 == 0)
                            {
                                await dbContext.SaveChangesAsync();
                            }
                        }
                        await dbContext.SaveChangesAsync();
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
            var cache = await dbContext.Tiles
                .Where(p => p.TemplateUrl == Url && p.X == column && p.Y == row && p.Z == level)
                .FirstOrDefaultAsync(cancellationToken);
            if (cache != null)
            {
                string file = Path.Combine(CacheFileDir, Guid.NewGuid().ToString());
                await File.WriteAllBytesAsync(file, cache.Data, cancellationToken);
                return new Uri(file, UriKind.Absolute);
            }
            else
            {
                string url = Url.Replace("{x}", column.ToString()).Replace("{y}", row.ToString()).Replace("{z}", level.ToString()).Trim();
                cacheQueue.Enqueue(new TileCacheEntity()
                {
                    X = column,
                    Y = row,
                    Z = level,
                    TemplateUrl = Url,
                    TileUrl = url,
                });
                return new Uri(url);
            }
        }
    }

}
