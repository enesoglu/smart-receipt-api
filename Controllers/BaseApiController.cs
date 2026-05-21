using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace smart_receipt_api.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
        }
    }
}
