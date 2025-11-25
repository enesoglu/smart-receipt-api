using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_receipt_api.Data;
using smart_receipt_api.Models;

namespace smart_receipt_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // (GET: api/users)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // (POST: api/users)
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            // if password is not provided, set a default password
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = "12345";
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsers", new { id = user.Id }, user);
        }
    }
}