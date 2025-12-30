using smart_receipt_api.Models;

namespace smart_receipt_api.Services
{
    public interface IUserService
    {
        Task<User?> RegisterAsync(string username, string password);
        Task<User?> LoginAsync(string username, string password);
        Task<User?> GetUserByIdAsync(int id);
    }
}

