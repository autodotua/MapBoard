using MapBoard.IO;
using MapBoard.Mapping;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using static MapBoard.Mapping.CacheableWebTiledLayer;

namespace MapBoard.IO.TrackDb
{
    public class TrackDbContext : DbContext
    {
        public TrackDbContext()
        {

        }
        public DbSet<TrackEntity> Tracks { get; set; }
        public DbSet<TrackPointEntity> Points { get; set; }

        public static async Task InitializeAsync()
        {
            using var db = new TrackDbContext();
            await db.Database.EnsureCreatedAsync();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={Path.Combine(FolderPaths.TrackPath, "tracks.db")}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
