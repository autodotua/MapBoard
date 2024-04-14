using MapBoard.GeoShare.Core.Dto;
using MapBoard.GeoShare.Core.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.GeoShare.Core.Service
{
    public class SharedLocationService(GeoShareDbContext db, UserService userService)
    {
        public async Task<IList<UserLocationDto>> GetGroupLastLocationAsync(string groupName)
        {
            var latestLocations = db.SharedLocations
           // 首先按用户ID进行分组
           .GroupBy(sl => sl.UserId)
           .Select(g => new
           {
               UserId = g.Key,
               // 在分组中按时间降序排序，选择每组中的第一条记录（即最新记录）
               LatestLocation = g.OrderByDescending(sl => sl.Time).FirstOrDefault()
           })
           .ToList();
            var groupUsers =await userService.GetSameGroupUsersAsync(groupName);
            return latestLocations
                .Where(p => groupUsers.ContainsKey(p.UserId))
                .Select(p => new UserLocationDto()
                {
                    UserName = groupUsers[p.UserId],
                    Location = p.LatestLocation,
                })
                .ToList();
        }

        public async Task<SharedLocationEntity> InsertCurrentLocation(string username, double longitude, double latitude, double altitude
#if DEBUG
            , DateTime time=default
#endif
            )
        {
            UserEntity user = await userService.GetUserAsync(username);
            SharedLocationEntity entity = new SharedLocationEntity()
            {
                Longitude = longitude,
                Latitude = latitude,
                Altitude = altitude,
                UserId = user.Id,
                Time = DateTime.Now,
            };
#if DEBUG
           if(time!= default)
            {
                entity.Time = time;
            }
#endif
            db.SharedLocations.Add(entity);
            await db.SaveChangesAsync();
            return entity;
        }
    }
}
