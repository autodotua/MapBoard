using MapBoard.GeoShare.Core.Dto;
using MapBoard.GeoShare.Core.Entity;
using MapBoard.GeoShare.Core.Service;
using Microsoft.AspNetCore.Mvc;

namespace MapBoard.GeoShare.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SharedLocationController(SharedLocationService sharedLocationService, UserService userService) : GeoShareControllerBase
    {
        private readonly SharedLocationService sharedLocationService = sharedLocationService;
        private readonly UserService userService = userService;

        [HttpGet("LatestLocations")]
        public async Task<IList<UserLocationDto>> GetLatestLocationsAsync()
        {
            return await sharedLocationService.GetGroupLastLocationAsync((await userService.GetUserAsync(GetUser())).GroupName);
        }

        [HttpPost("NewLocation")]
        public async Task ReportNewLocationAsync(SharedLocationEntity location)
        {
            await sharedLocationService.InsertCurrentLocation(GetUser(), location.Longitude, location.Latitude, location.Altitude);
        }
    }
}
