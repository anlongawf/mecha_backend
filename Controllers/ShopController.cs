using Microsoft.AspNetCore.Mvc;
using Mecha.DTO;
using Mecha.Helpers;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Mecha.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly SqlConnectionHelper _sqlHelper;

        public ShopController(SqlConnectionHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        // GET /api/Shop/products
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] string? category = null, [FromQuery] int? userId = null)
        {
            try
            {
                var sql = @"
                    SELECT p.*, 
                           CASE WHEN ue.effect_id IS NOT NULL THEN 1 ELSE 0 END as IsOwned,
                           CASE WHEN ue.is_active = 1 THEN 1 ELSE 0 END as IsApplied
                    FROM products p
                    LEFT JOIN user_effects ue ON p.product_id = ue.product_id AND ue.idUser = @userId
                    WHERE p.is_active = 1";

                var parameters = new List<MySqlParameter> { _sqlHelper.CreateParameter("@userId", userId ?? 0) };

                if (!string.IsNullOrEmpty(category))
                {
                    sql += " AND p.category = @category";
                    parameters.Add(_sqlHelper.CreateParameter("@category", category));
                }

                sql += " ORDER BY p.price ASC, p.name ASC";

                var products = new List<ProductDto>();
                using var reader = await _sqlHelper.ExecuteReaderAsync(sql, parameters.ToArray());

                while (await reader.ReadAsync())
                {
                    products.Add(new ProductDto
                    {
                        ProductId = Convert.ToInt32(reader["product_id"]),
                        Name = reader["name"]?.ToString() ?? "",
                        Description = reader["description"]?.ToString(),
                        Type = reader["type"]?.ToString() ?? "effect",
                        Category = reader["category"]?.ToString(),
                        Price = Convert.ToDecimal(reader["price"]),
                        PremiumOnly = Convert.ToBoolean(reader["premium_only"]),
                        Icon = reader["icon"]?.ToString(),
                        PreviewImage = reader["preview_image"]?.ToString(),
                        EffectData = reader["effect_data"]?.ToString(),
                        IsActive = Convert.ToBoolean(reader["is_active"]),
                        IsOwned = Convert.ToInt32(reader["IsOwned"]) == 1,
                        IsApplied = Convert.ToInt32(reader["IsApplied"]) == 1
                    });
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving products", error = ex.Message });
            }
        }

        // GET /api/Shop/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProduct(int productId, [FromQuery] int? userId = null)
        {
            try
            {
                var sql = @"
                    SELECT p.*, 
                           CASE WHEN ue.effect_id IS NOT NULL THEN 1 ELSE 0 END as IsOwned,
                           CASE WHEN ue.is_active = 1 THEN 1 ELSE 0 END as IsApplied
                    FROM products p
                    LEFT JOIN user_effects ue ON p.product_id = ue.product_id AND ue.idUser = @userId
                    WHERE p.product_id = @productId AND p.is_active = 1";

                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@productId", productId),
                    _sqlHelper.CreateParameter("@userId", userId ?? 0));

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Product not found" });

                var product = new ProductDto
                {
                    ProductId = Convert.ToInt32(reader["product_id"]),
                    Name = reader["name"]?.ToString() ?? "",
                    Description = reader["description"]?.ToString(),
                    Type = reader["type"]?.ToString() ?? "effect",
                    Category = reader["category"]?.ToString(),
                    Price = Convert.ToDecimal(reader["price"]),
                    PremiumOnly = Convert.ToBoolean(reader["premium_only"]),
                    Icon = reader["icon"]?.ToString(),
                    PreviewImage = reader["preview_image"]?.ToString(),
                    EffectData = reader["effect_data"]?.ToString(),
                    IsActive = Convert.ToBoolean(reader["is_active"]),
                    IsOwned = Convert.ToInt32(reader["IsOwned"]) == 1,
                    IsApplied = Convert.ToInt32(reader["IsApplied"]) == 1
                };

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving product", error = ex.Message });
            }
        }

        // POST /api/Shop/purchase
        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseProduct([FromBody] PurchaseRequest request)
        {
            try
            {
                // Check if user exists
                var userCheckSql = "SELECT Premium FROM users WHERE IdUser = @userId";
                var isPremium = false;
                using (var reader = await _sqlHelper.ExecuteReaderAsync(userCheckSql,
                    _sqlHelper.CreateParameter("@userId", request.UserId)))
                {
                    if (await reader.ReadAsync())
                    {
                        isPremium = Convert.ToBoolean(reader["Premium"]);
                    }
                    else
                    {
                        return NotFound(new { message = "User not found" });
                    }
                }

                // Get product
                var productSql = "SELECT * FROM products WHERE product_id = @productId AND is_active = 1";
                ProductDto? product = null;
                using (var reader = await _sqlHelper.ExecuteReaderAsync(productSql,
                    _sqlHelper.CreateParameter("@productId", request.ProductId)))
                {
                    if (await reader.ReadAsync())
                    {
                        product = new ProductDto
                        {
                            ProductId = Convert.ToInt32(reader["product_id"]),
                            Name = reader["name"]?.ToString() ?? "",
                            Price = Convert.ToDecimal(reader["price"]),
                            PremiumOnly = Convert.ToBoolean(reader["premium_only"])
                        };
                    }
                }

                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Check premium requirement
                if (product.PremiumOnly && !isPremium)
                    return StatusCode(403, new { message = "Premium subscription required for this product" });

                // Check if already owned
                var ownedCheckSql = "SELECT COUNT(*) FROM user_effects WHERE idUser = @userId AND product_id = @productId";
                var ownedCount = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(ownedCheckSql,
                    _sqlHelper.CreateParameter("@userId", request.UserId),
                    _sqlHelper.CreateParameter("@productId", request.ProductId)));

                if (ownedCount > 0)
                    return BadRequest(new { message = "Product already owned" });

                // For now, we'll allow free purchases (can integrate payment later)
                // Create purchase record
                var purchaseSql = @"
                    INSERT INTO purchases (idUser, product_id, price, payment_method, transaction_id, status)
                    VALUES (@userId, @productId, @price, @paymentMethod, @transactionId, 'completed')";

                var transactionId = Guid.NewGuid().ToString();
                await _sqlHelper.ExecuteNonQueryAsync(purchaseSql,
                    _sqlHelper.CreateParameter("@userId", request.UserId),
                    _sqlHelper.CreateParameter("@productId", request.ProductId),
                    _sqlHelper.CreateParameter("@price", product.Price),
                    _sqlHelper.CreateParameter("@paymentMethod", request.PaymentMethod ?? "free"),
                    _sqlHelper.CreateParameter("@transactionId", transactionId));

                // Add to user_effects
                var effectSql = @"
                    INSERT INTO user_effects (idUser, product_id, is_active, applied_to, effect_settings)
                    VALUES (@userId, @productId, 0, 'profile', NULL)";

                await _sqlHelper.ExecuteNonQueryAsync(effectSql,
                    _sqlHelper.CreateParameter("@userId", request.UserId),
                    _sqlHelper.CreateParameter("@productId", request.ProductId));

                return Ok(new { message = "Purchase successful", transactionId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing purchase", error = ex.Message });
            }
        }

        // GET /api/Shop/user/{userId}/effects
        [HttpGet("user/{userId}/effects")]
        public async Task<IActionResult> GetUserEffects(int userId)
        {
            try
            {
                var sql = @"
                    SELECT ue.*, p.name as product_name
                    FROM user_effects ue
                    INNER JOIN products p ON ue.product_id = p.product_id
                    WHERE ue.idUser = @userId
                    ORDER BY ue.applied_at DESC";

                var effects = new List<UserEffectDto>();
                using var reader = await _sqlHelper.ExecuteReaderAsync(sql,
                    _sqlHelper.CreateParameter("@userId", userId));

                while (await reader.ReadAsync())
                {
                    effects.Add(new UserEffectDto
                    {
                        EffectId = Convert.ToInt32(reader["effect_id"]),
                        UserId = Convert.ToInt32(reader["idUser"]),
                        ProductId = Convert.ToInt32(reader["product_id"]),
                        ProductName = reader["product_name"]?.ToString() ?? "",
                        IsActive = Convert.ToBoolean(reader["is_active"]),
                        AppliedTo = reader["applied_to"]?.ToString() ?? "profile",
                        EffectSettings = reader["effect_settings"]?.ToString(),
                        AppliedAt = Convert.ToDateTime(reader["applied_at"])
                    });
                }

                return Ok(effects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user effects", error = ex.Message });
            }
        }

        // PUT /api/Shop/user/{userId}/effect/{effectId}/apply
        [HttpPut("user/{userId}/effect/{effectId}/apply")]
        public async Task<IActionResult> ApplyEffect(int userId, int effectId, [FromBody] ApplyEffectRequest request)
        {
            try
            {
                // Verify ownership
                var checkSql = "SELECT COUNT(*) FROM user_effects WHERE effect_id = @effectId AND idUser = @userId";
                var count = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@effectId", effectId),
                    _sqlHelper.CreateParameter("@userId", userId)));

                if (count == 0)
                    return NotFound(new { message = "Effect not found or not owned by user" });

                // Deactivate all other effects of the same type
                var deactivateSql = @"
                    UPDATE user_effects 
                    SET is_active = 0 
                    WHERE idUser = @userId AND applied_to = @appliedTo AND effect_id != @effectId";

                await _sqlHelper.ExecuteNonQueryAsync(deactivateSql,
                    _sqlHelper.CreateParameter("@userId", userId),
                    _sqlHelper.CreateParameter("@appliedTo", request.AppliedTo ?? "profile"),
                    _sqlHelper.CreateParameter("@effectId", effectId));

                // Activate this effect
                var activateSql = @"
                    UPDATE user_effects 
                    SET is_active = 1, applied_to = @appliedTo, effect_settings = @settings
                    WHERE effect_id = @effectId";

                var settingsJson = request.Settings != null ? JsonSerializer.Serialize(request.Settings) : null;
                await _sqlHelper.ExecuteNonQueryAsync(activateSql,
                    _sqlHelper.CreateParameter("@effectId", effectId),
                    _sqlHelper.CreateParameter("@appliedTo", request.AppliedTo ?? "profile"),
                    _sqlHelper.CreateParameter("@settings", settingsJson ?? (object)DBNull.Value));

                return Ok(new { message = "Effect applied successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error applying effect", error = ex.Message });
            }
        }

        // DELETE /api/Shop/user/{userId}/effect/{effectId}
        [HttpDelete("user/{userId}/effect/{effectId}")]
        public async Task<IActionResult> RemoveEffect(int userId, int effectId)
        {
            try
            {
                // Verify ownership
                var checkSql = "SELECT COUNT(*) FROM user_effects WHERE effect_id = @effectId AND idUser = @userId";
                var count = Convert.ToInt32(await _sqlHelper.ExecuteScalarAsync(checkSql,
                    _sqlHelper.CreateParameter("@effectId", effectId),
                    _sqlHelper.CreateParameter("@userId", userId)));

                if (count == 0)
                    return NotFound(new { message = "Effect not found or not owned by user" });

                // Deactivate instead of delete (so user keeps the purchase)
                var deactivateSql = "UPDATE user_effects SET is_active = 0 WHERE effect_id = @effectId AND idUser = @userId";
                await _sqlHelper.ExecuteNonQueryAsync(deactivateSql,
                    _sqlHelper.CreateParameter("@effectId", effectId),
                    _sqlHelper.CreateParameter("@userId", userId));

                return Ok(new { message = "Effect removed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error removing effect", error = ex.Message });
            }
        }
    }

    public class PurchaseRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class ApplyEffectRequest
    {
        public string? AppliedTo { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }
}

