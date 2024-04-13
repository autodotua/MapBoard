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
        //public async Task<SharedLocationEntity> GetUserLastLocationAsync(string username)
        //{
        //    UserEntity user = await userService.GetUserAsync(username);
        //    return await db.SharedLocations.WhereNotDeleted()
        //        .Where(p => p.UserId == user.Id)
        //        .OrderByDescending(p => p.Time)
        //        .FirstOrDefaultAsync();
        //}

        public async Task<IList<UserLocationDto>> GetGroupLastLocationAsync(string groupName)
        {
            var query = from location in db.SharedLocations
                        join user in db.Users on location.UserId equals user.Id
                        join grp in db.Groups on user.GroupId equals grp.Id
                        where grp.GroupName == groupName
                        group new { location, user } by location.UserId into userGroup
                        let latestLocation = userGroup.OrderByDescending(g => g.location.Time).FirstOrDefault()
                        select new UserLocationDto()
                        {
                            User = latestLocation.user,
                            Location = latestLocation.location
                        };
            return await query.ToListAsync();
        }

        public async Task<SharedLocationEntity> InsertCurrentLocation(string username, double longitude, double latitude, double altitude)
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
            db.SharedLocations.Add(entity);
            await db.SaveChangesAsync();
            return entity;
        }
    }
}
