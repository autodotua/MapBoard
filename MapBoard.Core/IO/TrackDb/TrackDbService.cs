using MapBoard.IO.Gpx;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapBoard.IO.TrackDb
{
    public class TrackDbService
    {
        public TrackEntity Track { get; private set; } = new TrackEntity();
        public IList<TrackPointEntity> TrackPoints { get; private set; } = new List<TrackPointEntity>();
        private TrackDbService()
        {
        }

        public async static Task<TrackDbService> FromTrackAsync(TrackEntity track)
        {
            TrackDbService service = new TrackDbService();
            service.Track = track;
            service.TrackPoints = await GetTrackPointsAsync(track.Id);
            return service;
        }

        public static async Task<TrackDbService> CreateTrackAsync()
        {
            TrackDbService service = new TrackDbService();
            using var db = new TrackDbContext();
            TrackEntity track = new TrackEntity();
            db.Tracks.Add(track);
            await db.SaveChangesAsync();
            service.Track = track;
            return service;

        }

        public async Task AddPointAsync(GpxPoint point)
        {
            using var db = new TrackDbContext();
            var dbPoint = point.ToTrackDbPoint();
            dbPoint.TrackId = Track.Id;
            db.Points.Add(dbPoint);
            await db.SaveChangesAsync();
        }

        public async static Task<IList<TrackEntity>> GetTracksAsync(int limit = int.MaxValue)
        {
            using var db = new TrackDbContext();
            return await db.Tracks
                     .OrderByDescending(p => p.StartTime)
                     .Take(limit)
                     .ToListAsync();
        }

        private async static Task<IList<TrackPointEntity>> GetTrackPointsAsync(int trackId)
        {
            using var db = new TrackDbContext();
            return await db.Points
                .Where(p => p.TrackId == trackId)
                .OrderBy(p => p.Time)
                .ToListAsync();
        }

        public async Task SaveTrackAsync()
        {
            using var db = new TrackDbContext();
            var existed = await db.Tracks.FindAsync(Track.Id) ?? throw new ArgumentException($"找不到ID为{Track.Id}的成果");
            Track.Adapt(existed);
            await db.SaveChangesAsync();
        }
    }
}
