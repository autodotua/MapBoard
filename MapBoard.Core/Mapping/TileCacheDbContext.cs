using MapBoard.IO;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using static MapBoard.Mapping.CacheableWebTiledLayer;

namespace MapBoard.Mapping
{
    public class TileCacheDbContext : DbContext
    {
        private static object lockObj = new object();
        public TileCacheDbContext()
        {
            lock (lockObj)
            {
                try
                {
                    Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    Database.EnsureDeleted();
                    Database.EnsureCreated();
                }
            }
        }
        public DbSet<TileCacheEntity> Tiles { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Path.Combine(FolderPaths.CachePath, "tiles.db")}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }


}
