using EPApi.Models;

namespace EPApi.DataAccess
{
    public interface IUserRepository
    {
        Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default);
        Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default);
        Task<int> CreateAsync(User user, CancellationToken ct = default);
    }
}