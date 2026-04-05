using IdentityService.Models;

namespace IdentityService.Data;

public interface IUserRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string email, string passwordHash, string fullName, CancellationToken cancellationToken = default);
}
