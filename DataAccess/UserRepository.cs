using System.Data;
using Microsoft.Data.SqlClient;
using EPApi.Models;
using Microsoft.Extensions.Configuration;

namespace EPApi.DataAccess
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("Default") ??
                throw new InvalidOperationException("Missing ConnectionStrings:Default");
        }

        public async Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TOP 1 Id, UserName, PasswordHash, Role, CreatedAt
                                FROM dbo.Users WHERE UserName = @userName";
            cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 100) { Value = userName });

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            return new User
            {
                Id = reader.GetInt32(0),
                UserName = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Role = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        public async Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM dbo.Users WHERE UserName = @userName";
            cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 100) { Value = userName });
            var result = await cmd.ExecuteScalarAsync(ct);
            return result != null;
        }

        public async Task<int> CreateAsync(User user, CancellationToken ct = default)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO dbo.Users(UserName, PasswordHash, Role)
OUTPUT INSERTED.Id
VALUES (@userName, @passwordHash, @role);";
            cmd.Parameters.Add(new SqlParameter("@userName", SqlDbType.NVarChar, 100) { Value = user.UserName });
            cmd.Parameters.Add(new SqlParameter("@passwordHash", SqlDbType.NVarChar, 400) { Value = user.PasswordHash });
            cmd.Parameters.Add(new SqlParameter("@role", SqlDbType.NVarChar, 50) { Value = (object?)user.Role ?? DBNull.Value });

            var insertedId = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(insertedId);
        }
    }
}