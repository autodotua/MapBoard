using MapBoard.GeoShare.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MapBoard.GeoShare.Core.Service
{
    public class UserService(GeoShareDbContext db, IMemoryCache memoryCache)
    {
        private const string usersCacheKey = "Users";
        private GeoShareDbContext db = db;
        private IMemoryCache memoryCache = memoryCache;
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

            cacheUsers.Add(newUser.Id, newUser);

            return newUser;
        }

        public async Task<Dictionary<int, string>> GetSameGroupUsersAsync(string groupName)
        {
            var users = await GetUsersAsync();
            return users.Values.Where(p => p.GroupName == groupName).ToDictionary(p => p.Id, p => p.Username);
        }

        public async Task<UserEntity> GetUserAsync(string username)
        {
            var users = await GetUsersAsync();
            return users.Values.FirstOrDefault(p => p.Username == username);
        }

        public async Task<UserEntity> GetUserAsync(int id)
        {
            var users = await GetUsersAsync();
            if (users.TryGetValue(id, out UserEntity user))
            {
                return user;
            }
            throw new StatusBasedException($"找不到ID为{id}的用户", System.Net.HttpStatusCode.NotFound);
        }

        public async Task<Dictionary<int, UserEntity>> GetUsersAsync()
        {
            if (memoryCache.TryGetValue(usersCacheKey, out Dictionary<int, UserEntity> cacheUsers))
            {
                return cacheUsers;
            }
            var users = await db.Users.ToDictionaryAsync(p => p.Id);
            memoryCache.Set(usersCacheKey, users);
            return users;
        }
        public async Task<int> RegisterAsync(UserEntity user)
        {
            var users = await GetUsersAsync();
            if (users.Values.Any(p => p.Username == user.Username))
            {
                throw new StatusBasedException("用户名重复",System.Net.HttpStatusCode.Conflict);
            }
            user.Id = 0;
            db.Users.Add(user);
            await db.SaveChangesAsync();
            users.Add(user.Id, user);
            return user.Id;
        }
        public async Task UpdateGroupNameAsync(int userId, string groupName)
        {
            var users = await GetUsersAsync();

            if (users.TryGetValue(userId, out UserEntity user))
            {
                user.GroupName = groupName;
                db.Entry(user).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
            else
            {
                throw new StatusBasedException($"找不到ID为{userId}的用户",System.Net.HttpStatusCode.NotFound);
            }
        }
    }
}
