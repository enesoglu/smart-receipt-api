using smart_receipt_api.Models;

namespace smart_receipt_api.Services
{
    public interface IUserService
    {
        Task<User?> RegisterAsync(string fullName, string email, string password);
        Task<User?> LoginAsync(string email, string password);
        Task<User?> GetUserByIdAsync(int id);
    }
}
