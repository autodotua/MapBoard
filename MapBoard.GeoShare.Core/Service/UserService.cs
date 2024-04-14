using MapBoard.GeoShare.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MapBoard.GeoShare.Core.Service
{
    public class UserService(GeoShareDbContext db, IMemoryCache memoryCache)
    {
        private GeoShareDbContext db = db;
        private IMemoryCache memoryCache = memoryCache;
        private const string usersCacheKey = "Users";

        private async Task<List<UserEntity>> GetUsersAsync()
        {
            if (memoryCache.TryGetValue(usersCacheKey, out List<UserEntity> cacheUsers))
            {
                return cacheUsers;
            }
            var users = await db.Users.ToListAsync();
            memoryCache.Set(usersCacheKey, users);
            return users;
        }

        public async Task<UserEntity> GetUserAsync(string username)
        {
            var users = await GetUsersAsync();
            return users.FirstOrDefault(p => p.Username == username);
        }

        public async Task<UserEntity> AddUserAsync(string username, string password, string groupName)
        {
            // 创建新用户实体
            var newUser = new UserEntity
            {
                Username = username,
                Password = password,
                GroupName = groupName
            };

            var cacheUsers = await GetUsersAsync();
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            cacheUsers.Add(newUser);

            return newUser;
        }

        public async Task<Dictionary<int, string>> GetSameGroupUsersAsync(string groupName)
        {
            var users = await GetUsersAsync();
            return users.Where(p => p.GroupName == groupName).ToDictionary(p => p.Id, p => p.Username);
        }
    }
}
