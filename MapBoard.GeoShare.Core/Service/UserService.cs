using MapBoard.GeoShare.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MapBoard.GeoShare.Core.Service
{
    public class UserService(GeoShareDbContext dbContext, IMemoryCache memoryCache)
    {
        private GeoShareDbContext dbContext = dbContext;
        private IMemoryCache memoryCache = memoryCache;

        // 使用缓存来优化 GetUserAsync 方法
        public async Task<UserEntity> GetUserAsync(int id)
        {
            // 尝试从缓存中获取用户
            if (memoryCache.TryGetValue(id, out UserEntity user))
            {
                return user;
            }

            // 如果缓存中没有找到，则从数据库中查询
            user = await dbContext.Users.FindAsync(id);

            // 将查询结果存入缓存
            if (user != null)
            {
                memoryCache.Set(id, user);
            }

            return user;
        }

        public async Task<UserEntity> GetUserAsync(string username)
        {
            // 使用用户名作为缓存键
            string cacheKey = $"User_{username}";

            // 尝试从缓存中获取用户
            if (memoryCache.TryGetValue(cacheKey, out UserEntity user))
            {
                return user;
            }

            // 如果缓存中没有找到，则从数据库中查询
            user = await dbContext.Users.WhereNotDeleted().FirstOrDefaultAsync(p => p.Username == username);

            // 将查询结果存入缓存
            if (user != null)
            {
                memoryCache.Set(cacheKey, user);
            }

            return user;
        }

        public async Task<UserEntity> AddUser(string username, string password)
        {
            // 创建新用户实体
            var newUser = new UserEntity
            {
                Username = username,
                Password = password,
                GroupId = default, // 根据需求设置 GroupId 的默认值
                IsDeleted = false // 默认情况下，新用户未被删除
            };

            // 添加新用户到数据库
            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();

            // 将新用户添加到缓存
            memoryCache.Set(newUser.Id, newUser);
            string cacheKey = $"User_{newUser.Username}";
            memoryCache.Set(cacheKey, newUser);

            return newUser;
        }

        // 修改用户密码的方法
        public async Task ChangeUserPasswordAsync(int userId, string newPassword)
        {
            // 从数据库中获取用户实体
            var user = await dbContext.Users.FindAsync(userId);

            if (user != null && !user.IsDeleted)
            {
                // 修改用户密码
                user.Password = newPassword;

                // 保存更改
                await dbContext.SaveChangesAsync();

                // 更新缓存中的用户实体
                memoryCache.Set(userId, user);
                string cacheKey = $"User_{user.Username}";
                memoryCache.Set(cacheKey, user);
            }
        }
    }
}
