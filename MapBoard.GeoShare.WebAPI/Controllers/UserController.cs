using MapBoard.GeoShare.Core.Entity;
using MapBoard.GeoShare.Core.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MapBoard.GeoShare.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController(UserService userService) : GeoShareControllerBase
    {
        private readonly UserService userService = userService;

        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync(UserEntity user)
        {
            var dbUser = await userService.GetUserAsync(user.Username);
            if (dbUser == null)
            {
                return Unauthorized("用户不存在");
            }
            if (dbUser.Password != user.Password)
            {
                return Unauthorized("用户名和密码不匹配");
            }
            if (user.GroupName != dbUser.GroupName)
            {
                await userService.UpdateGroupNameAsync(dbUser.Id, user.GroupName);
            }
            HttpContext.Session.SetInt32("user", dbUser.Id);
            return Ok();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> RegisterAsync(UserEntity user)
        {
            int id;
            try
            {
                id = await userService.RegisterAsync(user);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
            return Ok(id);
        }
    }
}
