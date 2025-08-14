using EPApi.DataAccess;

namespace EPApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _jwt;

        public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt)
        {
            _users = users;
            _hasher = hasher;
            _jwt = jwt;
        }

        public async Task<string?> LoginAsync(string userName, string password, CancellationToken ct = default)
        {
            var user = await _users.FindByUserNameAsync(userName, ct);
            if (user is null) return null;
            if (!_hasher.Verify(password, user.PasswordHash)) return null;
            return _jwt.GenerateToken(user);
        }
    }
}