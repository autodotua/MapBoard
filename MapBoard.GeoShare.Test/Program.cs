using MapBoard.GeoShare.Core;
using MapBoard.GeoShare.Core.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new ServiceCollection();
services.AddDbContext<GeoShareDbContext>();
services.AddTransient<SharedLocationService>();
services.AddTransient<UserService>();
services.AddMemoryCache();
var provider=services.BuildServiceProvider();

Console.WriteLine("清空数据库");
var db = provider.GetRequiredService<GeoShareDbContext>();
    await db.Database.ExecuteSqlRawAsync("delete from Users");
    await db.Database.ExecuteSqlRawAsync("delete from SharedLocations");


Console.WriteLine("增加测试用户");
var userService = provider.GetRequiredService<UserService>();
await userService.AddUserAsync("user1", "pswd","grp1");
await userService.AddUserAsync("user2", "pswd","grp2");
await userService.AddUserAsync("user3", "pswd","grp1");

//Console.WriteLine("新增位置");
//var locationService = provider.GetRequiredService<SharedLocationService>();
//await locationService.InsertCurrentLocation("user1", 120, 20, 40,new DateTime(2000,2,1));
//await locationService.InsertCurrentLocation("user1", 120, 20, 30,new DateTime(2000,1,1));
//await locationService.InsertCurrentLocation("user3", 120, 20, 30,new DateTime(2000,1,1));
//await locationService.InsertCurrentLocation("user2", 120, 20, 30,new DateTime(2000,1,1));

//Console.WriteLine("读取位置");
//var locations =await locationService.GetGroupLastLocationAsync("grp1");

