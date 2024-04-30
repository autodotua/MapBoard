using MapBoard.IO;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using static MapBoard.Mapping.CacheableWebTiledLayer;

namespace MapBoard.Mapping
{
    public class TileCacheDbContext : DbContext
    {
        private static object lockObj = new object();
        public TileCacheDbContext()
        {

        }
        public DbSet<TileCacheEntity> Tiles { get; set; }

        public static async Task InitializeAsync()
        {
            using var db = new TileCacheDbContext();
            await db.Database.EnsureCreatedAsync();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Path.Combine(FolderPaths.CachePath, "tiles.db")}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public async static Task ClearCacheAsync()
        {
            using TileCacheDbContext db = new TileCacheDbContext();
            await db.Tiles.ExecuteDeleteAsync();
            await db.Database.ExecuteSqlRawAsync("VACUUM;");
        }
    }
}
