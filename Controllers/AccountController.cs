using Microsoft.AspNetCore.Mvc;
using Mecha.DTO;
using Mecha.Helpers;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly SqlConnectionHelper _sqlHelper;

        public AccountController(SqlConnectionHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        // GET /api/Account/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAccount(int userId)
        {
            try
            {
                var sql = @"
                    SELECT u.IdUser, u.Username, u.Email, u.Phone, u.Roles, u.Premium, u.IsVerified, u.CreatedAt,
                           u.StyleId, s.username as ProfileUsername,
                           (SELECT COUNT(*) FROM purchases WHERE idUser = u.IdUser) as TotalPurchases,
                           (SELECT COUNT(*) FROM user_effects WHERE idUser = u.IdUser) as TotalEffects,
                           COALESCE(w.coins, 0) as Coins
                    FROM users u
                    LEFT JOIN style s ON u.StyleId = s.style_id
                    LEFT JOIN user_wallet w ON u.IdUser = w.idUser
                    WHERE u.IdUser = @userId";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@userId", userId));

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Account not found" });

                var account = new
                {
                    IdUser = Convert.ToInt32(reader["IdUser"]),
                    Username = reader["Username"]?.ToString(),
                    Email = reader["Email"]?.ToString(),
                    Phone = reader["Phone"]?.ToString(),
                    Roles = reader["Roles"]?.ToString() ?? "user",
                    Premium = Convert.ToBoolean(reader["Premium"]),
                    IsVerified = Convert.ToBoolean(reader["IsVerified"]),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                    StyleId = reader["StyleId"]?.ToString(),
                    ProfileUsername = reader["ProfileUsername"]?.ToString(),
                    TotalPurchases = Convert.ToInt32(reader["TotalPurchases"]),
                    TotalEffects = Convert.ToInt32(reader["TotalEffects"]),
                    Coins = Convert.ToInt32(reader["Coins"])
                };

                return Ok(account);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving account", error = ex.Message });
            }
        }

        // PUT /api/Account/{userId}
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateAccount(int userId, [FromBody] UpdateAccountRequest request)
        {
            try
            {
                var updates = new List<string>();
                var parameters = new List<MySqlParameter> { _sqlHelper.CreateParameter("@userId", userId) };

                if (!string.IsNullOrEmpty(request.Email))
                {
                    updates.Add("Email = @email");
                    parameters.Add(_sqlHelper.CreateParameter("@email", request.Email));
                }

                if (!string.IsNullOrEmpty(request.Phone))
                {
                    updates.Add("Phone = @phone");
                    parameters.Add(_sqlHelper.CreateParameter("@phone", request.Phone));
                }

                if (updates.Count == 0)
                    return BadRequest(new { message = "No fields to update" });

                var sql = $"UPDATE users SET {string.Join(", ", updates)} WHERE IdUser = @userId";
                await _sqlHelper.ExecuteNonQueryAsync(sql, parameters.ToArray());

                return Ok(new { message = "Account updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating account", error = ex.Message });
            }
        }

        // GET /api/Account/{userId}/purchases
        [HttpGet("{userId}/purchases")]
        public async Task<IActionResult> GetPurchaseHistory(int userId)
        {
            try
            {
                var sql = @"
                    SELECT p.purchase_id, p.idUser, p.product_id, pr.name as product_name, 
                           p.price, p.payment_method, p.transaction_id, p.status, p.purchased_at
                    FROM purchases p
                    INNER JOIN products pr ON p.product_id = pr.product_id
                    WHERE p.idUser = @userId
                    ORDER BY p.purchased_at DESC";

                var purchases = new List<PurchaseDto>();
                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@userId", userId));

                while (await reader.ReadAsync())
                {
                    purchases.Add(new PurchaseDto
                    {
                        PurchaseId = Convert.ToInt32(reader["purchase_id"]),
                        UserId = Convert.ToInt32(reader["idUser"]),
                        ProductId = Convert.ToInt32(reader["product_id"]),
                        ProductName = reader["product_name"]?.ToString() ?? "",
                        Price = Convert.ToDecimal(reader["price"]),
                        PaymentMethod = reader["payment_method"]?.ToString(),
                        TransactionId = reader["transaction_id"]?.ToString(),
                        Status = reader["status"]?.ToString() ?? "completed",
                        PurchasedAt = Convert.ToDateTime(reader["purchased_at"])
                    });
                }

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving purchase history", error = ex.Message });
            }
        }

        // GET /api/Account/{userId}/wallet
        [HttpGet("{userId}/wallet")]
        public async Task<IActionResult> GetWallet(int userId)
        {
            try
            {
                var sql = @"
                    SELECT wallet_id, idUser, coins, last_updated
                    FROM user_wallet
                    WHERE idUser = @userId";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@userId", userId));

                WalletDto? wallet = null;
                if (await reader.ReadAsync())
                {
                    wallet = new WalletDto
                    {
                        WalletId = Convert.ToInt32(reader["wallet_id"]),
                        UserId = Convert.ToInt32(reader["idUser"]),
                        Coins = Convert.ToInt32(reader["coins"]),
                        LastUpdated = Convert.ToDateTime(reader["last_updated"])
                    };
                }
                else
                {
                    // Create wallet if doesn't exist
                    var createSql = "INSERT INTO user_wallet (idUser, coins) VALUES (@userId, 0)";
                    await _sqlHelper.ExecuteNonQueryAsync(createSql,
                        _sqlHelper.CreateParameter("@userId", userId));

                    wallet = new WalletDto
                    {
                        WalletId = 0,
                        UserId = userId,
                        Coins = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                return Ok(wallet);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving wallet", error = ex.Message });
            }
        }

        // POST /api/Account/{userId}/wallet/add-coins
        [HttpPost("{userId}/wallet/add-coins")]
        public async Task<IActionResult> AddCoins(int userId, [FromBody] AddCoinsRequest request)
        {
            try
            {
                // Ensure wallet exists
                var checkSql = "SELECT COUNT(*) FROM user_wallet WHERE idUser = @userId";
                var exists = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@userId", userId))) > 0;

                if (!exists)
                {
                    var createSql = "INSERT INTO user_wallet (idUser, coins) VALUES (@userId, 0)";
                    await _sqlHelper.ExecuteNonQueryAsync(createSql,
                        _sqlHelper.CreateParameter("@userId", userId));
                }

                var updateSql = "UPDATE user_wallet SET coins = coins + @amount WHERE idUser = @userId";
                await _sqlHelper.ExecuteNonQueryAsync(updateSql,
                    _sqlHelper.CreateParameter("@userId", userId),
                    _sqlHelper.CreateParameter("@amount", request.Amount));

                return Ok(new { message = "Coins added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding coins", error = ex.Message });
            }
        }
    }

    public class UpdateAccountRequest
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class AddCoinsRequest
    {
        public int Amount { get; set; }
    }
}

