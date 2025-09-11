using System.Data;
using Microsoft.EntityFrameworkCore;
using Mecha.Data;

namespace Mecha.Helpers
{
    public class DatabaseHelper
    {
        private readonly AppDbContext _context;

        public DatabaseHelper(AppDbContext context)
        {
            _context = context;
        }

        public IDbDataParameter CreateParameter(IDbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }

        public async Task<(int? userId, string? currentUsername, string? currentStyleId)> GetUserInfoByIdAsync(int id)
        {
            var userSql = "SELECT IdUser, Username, StyleId FROM users WHERE IdUser = @userId";
            using var userCommand = _context.Database.GetDbConnection().CreateCommand();
            userCommand.CommandText = userSql;
            var userParam = CreateParameter(userCommand, "@userId", id);
            userCommand.Parameters.Add(userParam);

            await _context.Database.OpenConnectionAsync();

            using var reader = await userCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    userId: Convert.ToInt32(reader["IdUser"]),
                    currentUsername: reader["Username"]?.ToString(),
                    currentStyleId: reader["StyleId"]?.ToString()
                );
            }

            return (null, null, null);
        }

        public async Task<(int? userId, string? currentUsername, string? currentStyleId)> GetUserInfoByUsernameAsync(string username)
        {
            var userSql = "SELECT IdUser, Username, StyleId FROM users WHERE Username = @username";
            using var userCommand = _context.Database.GetDbConnection().CreateCommand();
            userCommand.CommandText = userSql;
            var userParam = CreateParameter(userCommand, "@username", username);
            userCommand.Parameters.Add(userParam);

            await _context.Database.OpenConnectionAsync();

            using var reader = await userCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    userId: Convert.ToInt32(reader["IdUser"]),
                    currentUsername: reader["Username"]?.ToString(),
                    currentStyleId: reader["StyleId"]?.ToString()
                );
            }

            return (null, null, null);
        }

        public async Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null)
        {
            var sql = excludeUserId.HasValue 
                ? "SELECT COUNT(*) FROM users WHERE Username = @username AND IdUser != @userId"
                : "SELECT COUNT(*) FROM users WHERE Username = @username";

            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(CreateParameter(command, "@username", username));
            
            if (excludeUserId.HasValue)
                command.Parameters.Add(CreateParameter(command, "@userId", excludeUserId.Value));

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task UpdateUsernameAsync(int userId, string newUsername)
        {
            var sql = "UPDATE users SET Username = @newUsername WHERE IdUser = @userId";
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(CreateParameter(command, "@newUsername", newUsername));
            command.Parameters.Add(CreateParameter(command, "@userId", userId));
            
            await command.ExecuteNonQueryAsync();
        }

        public async Task EnsureConnectionClosedAsync()
        {
            if (_context.Database.GetDbConnection().State == ConnectionState.Open)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}