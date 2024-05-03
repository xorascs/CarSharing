using CarSharing.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CarSharing.Controllers.Utilities
{
    public class Helper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public Helper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public bool IsAdminJoined()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("isAdmin") == 1;
        }
        public int? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext!.Session.GetInt32("Id");
        }
        public bool IsLoggedIn()
        {
            return !_httpContextAccessor.HttpContext!.Session.GetString("Name").IsNullOrEmpty();
        }
    }
}
