using MySql.Data.MySqlClient;

namespace Mecha.Helpers
{
    public class DatabaseHelper
    {
        private readonly SqlConnectionHelper _sqlHelper;

        public DatabaseHelper(SqlConnectionHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public async Task<(int? userId, string? currentUsername, string? currentStyleId)> GetUserInfoByIdAsync(int id)
        {
            var userSql = "SELECT IdUser, Username, StyleId FROM users WHERE IdUser = @userId";
            
            using var reader = await _sqlHelper.ExecuteReaderAsync(userSql, 
                _sqlHelper.CreateParameter("@userId", id));
            
            if (await reader.ReadAsync())
            {
                return (
                    userId: Convert.ToInt32(reader["IdUser"]),
                    currentUsername: reader["Username"] == DBNull.Value ? null : reader["Username"]?.ToString(),
                    currentStyleId: reader["StyleId"] == DBNull.Value ? null : reader["StyleId"]?.ToString()
                );
            }

            return (null, null, null);
        }

        public async Task<(int? userId, string? currentUsername, string? currentStyleId)> GetUserInfoByUsernameAsync(string username)
        {
            var userSql = "SELECT IdUser, Username, StyleId FROM users WHERE Username = @username";
            
            using var reader = await _sqlHelper.ExecuteReaderAsync(userSql, 
                _sqlHelper.CreateParameter("@username", username));
            
            if (await reader.ReadAsync())
            {
                return (
                    userId: Convert.ToInt32(reader["IdUser"]),
                    currentUsername: reader["Username"] == DBNull.Value ? null : reader["Username"]?.ToString(),
                    currentStyleId: reader["StyleId"] == DBNull.Value ? null : reader["StyleId"]?.ToString()
                );
            }

            return (null, null, null);
        }

        public async Task<bool> IsUsernameExistsAsync(string username, int? excludeUserId = null)
        {
            string sql;
            MySqlParameter[] parameters;

            if (excludeUserId.HasValue)
            {
                sql = "SELECT COUNT(*) FROM users WHERE Username = @username AND IdUser != @userId";
                parameters = new[]
                {
                    _sqlHelper.CreateParameter("@username", username),
                    _sqlHelper.CreateParameter("@userId", excludeUserId.Value)
                };
            }
            else
            {
                sql = "SELECT COUNT(*) FROM users WHERE Username = @username";
                parameters = new[] { _sqlHelper.CreateParameter("@username", username) };
            }

            var result = await _sqlHelper.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public async Task UpdateUsernameAsync(int userId, string newUsername)
        {
            var sql = "UPDATE users SET Username = @newUsername WHERE IdUser = @userId";
            await _sqlHelper.ExecuteNonQueryAsync(sql,
                _sqlHelper.CreateParameter("@newUsername", newUsername),
                _sqlHelper.CreateParameter("@userId", userId));
        }

        public async Task EnsureConnectionClosedAsync()
        {
            // Connection is managed by SqlConnectionHelper, no need to close manually
            await Task.CompletedTask;
        }
    }
}