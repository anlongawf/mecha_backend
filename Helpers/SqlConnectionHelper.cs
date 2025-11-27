using System.Data;
using MySql.Data.MySqlClient;

namespace Mecha.Helpers
{
    public class SqlConnectionHelper : IDisposable
    {
        private readonly string _connectionString;

        public SqlConnectionHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task<MySqlConnection> GetConnectionAsync()
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public MySqlParameter CreateParameter(string name, object? value)
        {
            return new MySqlParameter(name, value ?? DBNull.Value);
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params MySqlParameter[] parameters)
        {
            using var connection = await GetConnectionAsync();
            using var command = new MySqlCommand(sql, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<object?> ExecuteScalarAsync(string sql, params MySqlParameter[] parameters)
        {
            using var connection = await GetConnectionAsync();
            using var command = new MySqlCommand(sql, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            return await command.ExecuteScalarAsync();
        }

        public async Task<MySqlDataReader> ExecuteReaderAsync(string sql, params MySqlParameter[] parameters)
        {
            var connection = await GetConnectionAsync();
            var command = new MySqlCommand(sql, connection);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }
            // Use CommandBehavior.CloseConnection so reader closes connection when done
            return await command.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
        }

        public void Dispose()
        {
            // Connection pooling handles disposal automatically
        }
    }
}

