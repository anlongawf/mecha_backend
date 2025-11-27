using MySql.Data.MySqlClient;

namespace Mecha.Helpers
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Tắt foreign key checks tạm thời
                using var disableFkCheck = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0", connection);
                await disableFkCheck.ExecuteNonQueryAsync();

                // Tạo bảng style trước (không có foreign key)
                await CreateStyleTableAsync(connection);

                // Tạo bảng users (có foreign key đến style)
                await CreateUsersTableAsync(connection);

                // Tạo bảng user_styles (có foreign key đến users)
                await CreateUserStylesTableAsync(connection);

                // Tạo bảng cho shop và effects
                await CreateProductsTableAsync(connection);
                await CreatePurchasesTableAsync(connection);
                await CreateUserEffectsTableAsync(connection);
                await CreateUserWalletTableAsync(connection);

                // Tạo indexes
                await CreateIndexesAsync(connection);
                
                // Seed initial products
                await SeedInitialProductsAsync(connection);

                // Bật lại foreign key checks
                using var enableFkCheck = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1", connection);
                await enableFkCheck.ExecuteNonQueryAsync();

                Console.WriteLine("Database tables initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }

        private async Task CreateStyleTableAsync(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `style` (
                  `style_id` VARCHAR(50) NOT NULL,
                  `profile_avatar` VARCHAR(255) DEFAULT NULL,
                  `background` VARCHAR(255) DEFAULT NULL,
                  `audio` VARCHAR(255) DEFAULT NULL,
                  `AudioImage` VARCHAR(255) DEFAULT NULL,
                  `AudioTitle` VARCHAR(255) DEFAULT NULL,
                  `custom_cursor` VARCHAR(255) DEFAULT NULL,
                  `description` VARCHAR(500) DEFAULT NULL,
                  `username` VARCHAR(100) DEFAULT NULL,
                  `location` VARCHAR(255) DEFAULT NULL,
                  `Social` TEXT DEFAULT NULL,
                  PRIMARY KEY (`style_id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreateUsersTableAsync(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `users` (
                  `IdUser` INT(11) NOT NULL AUTO_INCREMENT,
                  `Username` VARCHAR(255) DEFAULT NULL,
                  `Email` VARCHAR(100) DEFAULT NULL,
                  `Phone` VARCHAR(10) DEFAULT NULL,
                  `password` VARCHAR(255) DEFAULT NULL,
                  `Roles` VARCHAR(50) DEFAULT 'user',
                  `DiscordId` VARCHAR(255) DEFAULT NULL,
                  `CreatedAt` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  `StyleId` VARCHAR(50) DEFAULT NULL,
                  `Premium` TINYINT(1) DEFAULT 0,
                  `IsVerified` TINYINT(1) DEFAULT 0,
                  PRIMARY KEY (`IdUser`),
                  UNIQUE KEY `UK_Username` (`Username`),
                  UNIQUE KEY `UK_Email` (`Email`),
                  UNIQUE KEY `UK_DiscordId` (`DiscordId`),
                  KEY `FK_StyleId` (`StyleId`),
                  CONSTRAINT `FK_User_Style` FOREIGN KEY (`StyleId`) REFERENCES `style` (`style_id`) ON DELETE SET NULL ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreateUserStylesTableAsync(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `user_styles` (
                  `style_id` INT(11) NOT NULL AUTO_INCREMENT,
                  `idUser` INT(11) NOT NULL,
                  `styles` JSON DEFAULT NULL,
                  PRIMARY KEY (`style_id`),
                  UNIQUE KEY `UK_UserStyle` (`idUser`),
                  CONSTRAINT `FK_UserStyle_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreateProductsTableAsync(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `products` (
                  `product_id` INT(11) NOT NULL AUTO_INCREMENT,
                  `name` VARCHAR(255) NOT NULL,
                  `description` TEXT DEFAULT NULL,
                  `type` VARCHAR(50) NOT NULL DEFAULT 'effect',
                  `category` VARCHAR(50) DEFAULT NULL,
                  `price` DECIMAL(10, 2) DEFAULT 0.00,
                  `premium_only` TINYINT(1) DEFAULT 0,
                  `icon` VARCHAR(255) DEFAULT NULL,
                  `preview_image` VARCHAR(255) DEFAULT NULL,
                  `effect_data` JSON DEFAULT NULL,
                  `is_active` TINYINT(1) DEFAULT 1,
                  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                  PRIMARY KEY (`product_id`),
                  KEY `IDX_Products_Type` (`type`),
                  KEY `IDX_Products_Category` (`category`),
                  KEY `IDX_Products_Active` (`is_active`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreatePurchasesTableAsync(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `purchases` (
                  `purchase_id` INT(11) NOT NULL AUTO_INCREMENT,
                  `idUser` INT(11) NOT NULL,
                  `product_id` INT(11) NOT NULL,
                  `price` DECIMAL(10, 2) NOT NULL,
                  `payment_method` VARCHAR(50) DEFAULT NULL,
                  `transaction_id` VARCHAR(255) DEFAULT NULL,
                  `status` VARCHAR(50) DEFAULT 'completed',
                  `purchased_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`purchase_id`),
                  KEY `FK_Purchase_User` (`idUser`),
                  KEY `FK_Purchase_Product` (`product_id`),
                  CONSTRAINT `FK_Purchase_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE,
                  CONSTRAINT `FK_Purchase_Product` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreateUserEffectsTableAsync(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `user_effects` (
                  `effect_id` INT(11) NOT NULL AUTO_INCREMENT,
                  `idUser` INT(11) NOT NULL,
                  `product_id` INT(11) NOT NULL,
                  `is_active` TINYINT(1) DEFAULT 1,
                  `applied_to` VARCHAR(50) DEFAULT 'profile',
                  `effect_settings` JSON DEFAULT NULL,
                  `applied_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`effect_id`),
                  KEY `FK_UserEffect_User` (`idUser`),
                  KEY `FK_UserEffect_Product` (`product_id`),
                  CONSTRAINT `FK_UserEffect_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE,
                  CONSTRAINT `FK_UserEffect_Product` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreateUserWalletTableAsync(MySqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `user_wallet` (
                  `wallet_id` INT(11) NOT NULL AUTO_INCREMENT,
                  `idUser` INT(11) NOT NULL,
                  `coins` INT(11) DEFAULT 0,
                  `last_updated` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                  PRIMARY KEY (`wallet_id`),
                  UNIQUE KEY `UK_UserWallet` (`idUser`),
                  CONSTRAINT `FK_Wallet_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task SeedInitialProductsAsync(MySqlConnection connection)
        {
            // First, update all existing products to be free
            var updateSql = "UPDATE `products` SET `price` = 0.00, `premium_only` = 0 WHERE `price` > 0 OR `premium_only` = 1";
            using var updateCommand = new MySqlCommand(updateSql, connection);
            try
            {
                await updateCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Updated all products to be free");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not update products to free: {ex.Message}");
            }
            
            // Update Matrix Rain to Hack
            var updateMatrixSql = "UPDATE `products` SET `name` = 'Hack', `description` = 'Hacker-style code rain effect', `effect_data` = '{\"type\":\"matrix\",\"color\":\"#00FF41\"}' WHERE `name` LIKE '%Matrix%' OR `name` LIKE '%matrix%'";
            using var updateMatrixCommand = new MySqlCommand(updateMatrixSql, connection);
            try
            {
                await updateMatrixCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✅ Updated Matrix Rain to Hack");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not update Matrix Rain: {ex.Message}");
            }
            
            // Delete Cursor Trail product
            var deleteCursorTrailSql = "DELETE FROM `products` WHERE `name` LIKE '%Cursor Trail%' OR `name` LIKE '%cursor trail%' OR (`category` = 'cursor' AND `name` LIKE '%Trail%')";
            using var deleteCursorTrailCommand = new MySqlCommand(deleteCursorTrailSql, connection);
            try
            {
                var deletedRows = await deleteCursorTrailCommand.ExecuteNonQueryAsync();
                if (deletedRows > 0)
                {
                    Console.WriteLine($"✅ Deleted {deletedRows} Cursor Trail product(s)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete Cursor Trail: {ex.Message}");
            }
            
            // Delete Pulse Animation product
            var deletePulseSql = "DELETE FROM `products` WHERE `name` LIKE '%Pulse Animation%' OR `name` LIKE '%pulse animation%'";
            using var deletePulseCommand = new MySqlCommand(deletePulseSql, connection);
            try
            {
                var deletedRows = await deletePulseCommand.ExecuteNonQueryAsync();
                if (deletedRows > 0)
                {
                    Console.WriteLine($"✅ Deleted {deletedRows} Pulse Animation product(s)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete Pulse Animation: {ex.Message}");
            }
            
            // Update Fireworks icon to proper fireworks emoji
            var updateFireworksIconSql = "UPDATE `products` SET `icon` = '🎆' WHERE `name` LIKE '%Fireworks%' OR `name` LIKE '%fireworks%' OR `description` LIKE '%fireworks%'";
            using var updateFireworksIconCommand = new MySqlCommand(updateFireworksIconSql, connection);
            try
            {
                var updatedRows = await updateFireworksIconCommand.ExecuteNonQueryAsync();
                if (updatedRows > 0)
                {
                    Console.WriteLine($"✅ Updated {updatedRows} Fireworks icon(s) to 🎆");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not update Fireworks icon: {ex.Message}");
            }
            
            // Check if products already exist
            var checkSql = "SELECT COUNT(*) FROM `products`";
            using var checkCommand = new MySqlCommand(checkSql, connection);
            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            
            if (count > 0) return; // Products already seeded

            var products = new (string name, string desc, string type, string category, decimal price, int premium, string icon, string? preview, string effectData)[]
            {
                // Free Text Animations
                ("Neon Glow", "Dynamic neon glow with color alternation", "effect", "text", 0.00m, 0, "💫", null, "{\"type\":\"neon-glow\",\"colors\":[\"#00FFFF\",\"#FF00FF\"],\"intensity\":\"high\"}"),
                ("Rainbow Gradient", "Smooth animated rainbow gradient text", "effect", "text", 0.00m, 0, "🌈", null, "{\"type\":\"rainbow-gradient\",\"speed\":\"slow\"}"),
                
                // Text Animations - All Free
                ("Dancing Shadow", "Dynamic shadow that dances around text", "effect", "text", 0.00m, 0, "👻", null, "{\"type\":\"dancing-shadow\",\"colors\":[\"#FF6B6B\",\"#4ECDC4\"],\"speed\":\"medium\"}"),
                ("Glitch Effect", "Cyber glitch distortion effect", "effect", "text", 0.00m, 0, "⚡", null, "{\"type\":\"glitch\",\"intensity\":\"medium\",\"frequency\":\"low\"}"),
                ("3D Spin", "3D rotating text with vibrant shadows", "effect", "text", 0.00m, 0, "🌀", null, "{\"type\":\"3d-spin\",\"speed\":\"medium\",\"depth\":\"high\"}"),
                ("Wavy Text", "Wave-like motion through letters", "effect", "text", 0.00m, 0, "🌊", null, "{\"type\":\"wavy\",\"amplitude\":\"medium\",\"speed\":\"slow\"}"),
                ("Text Masking", "Shimmering background clipped to text", "effect", "text", 0.00m, 0, "✨", null, "{\"type\":\"text-masking\",\"shimmer\":true,\"speed\":\"medium\"}"),
                ("Split Text", "Text slides in from opposite directions", "effect", "text", 0.00m, 0, "↔️", null, "{\"type\":\"split-text\",\"direction\":\"horizontal\",\"speed\":\"medium\"}"),
                ("Melting Text", "Text appears to melt and drip", "effect", "text", 0.00m, 0, "💧", null, "{\"type\":\"melting\",\"intensity\":\"medium\",\"speed\":\"slow\"}"),
                ("Explosive Burst", "Explosive burst effect on hover", "effect", "text", 0.00m, 0, "💥", null, "{\"type\":\"explosive-burst\",\"colors\":[\"#FF6B6B\",\"#FFE66D\"],\"intensity\":\"high\"}"),
                ("Holographic", "Futuristic holographic color shift", "effect", "text", 0.00m, 0, "🔮", null, "{\"type\":\"holographic\",\"colors\":[\"#FF006E\",\"#8338EC\",\"#3A86FF\"],\"shift\":\"smooth\"}"),
                ("Shimmer", "Elegant shimmer shine effect", "effect", "text", 0.00m, 0, "✨", null, "{\"type\":\"shimmer\",\"speed\":\"medium\",\"color\":\"#FFFFFF\"}"),
                ("Aurora", "Northern lights aurora effect", "effect", "text", 0.00m, 0, "🌌", null, "{\"type\":\"aurora\",\"colors\":[\"#00FF88\",\"#00D4FF\",\"#8B00FF\"]}"),
                ("Typewriter", "Classic typewriter typing effect", "effect", "text", 0.00m, 0, "⌨️", null, "{\"type\":\"typewriter\",\"speed\":\"medium\",\"blink\":true}"),
                ("Glowing 3D", "Glowing 3D text with rotation", "effect", "text", 0.00m, 0, "🌟", null, "{\"type\":\"glowing-3d\",\"rotation\":true,\"glow\":\"high\"}"),
                ("Text Shadow Animation", "Animated colorful text shadows", "effect", "text", 0.00m, 0, "🎭", null, "{\"type\":\"shadow-animation\",\"colors\":[\"#FF6B6B\",\"#4ECDC4\",\"#45B7D1\"],\"speed\":\"medium\"}"),
                ("Rainbow Spotlight", "Rainbow spotlight effect on text", "effect", "text", 0.00m, 0, "🔦", null, "{\"type\":\"rainbow-spotlight\",\"speed\":\"medium\",\"intensity\":\"high\"}"),
                
                // Colorful Text Animations - All Free
                ("Fire Gradient", "Fiery red-orange gradient animation", "effect", "text", 0.00m, 0, "🔥", null, "{\"type\":\"fire-gradient\",\"colors\":[\"#FF4500\",\"#FF6347\",\"#FFD700\"],\"speed\":\"fast\"}"),
                ("Ocean Wave", "Cool blue ocean wave gradient", "effect", "text", 0.00m, 0, "🌊", null, "{\"type\":\"ocean-wave\",\"colors\":[\"#00CED1\",\"#1E90FF\",\"#4169E1\"],\"speed\":\"medium\"}"),
                ("Sunset Glow", "Warm sunset orange-pink gradient", "effect", "text", 0.00m, 0, "🌅", null, "{\"type\":\"sunset-glow\",\"colors\":[\"#FF6347\",\"#FF69B4\",\"#FF1493\"],\"speed\":\"slow\"}"),
                ("Electric Blue", "Electric blue neon with pulse", "effect", "text", 0.00m, 0, "⚡", null, "{\"type\":\"electric-blue\",\"color\":\"#00BFFF\",\"intensity\":\"high\"}"),
                ("Purple Dream", "Dreamy purple-pink gradient", "effect", "text", 0.00m, 0, "💜", null, "{\"type\":\"purple-dream\",\"colors\":[\"#9370DB\",\"#BA55D3\",\"#DA70D6\"],\"speed\":\"medium\"}"),
                ("Golden Shine", "Luxurious gold shimmer effect", "effect", "text", 0.00m, 0, "✨", null, "{\"type\":\"golden-shine\",\"color\":\"#FFD700\",\"intensity\":\"high\"}"),
                ("Cyan Pulse", "Vibrant cyan pulsing glow", "effect", "text", 0.00m, 0, "💎", null, "{\"type\":\"cyan-pulse\",\"color\":\"#00FFFF\",\"speed\":\"medium\"}"),
                ("Magenta Flash", "Bold magenta flash animation", "effect", "text", 0.00m, 0, "💖", null, "{\"type\":\"magenta-flash\",\"color\":\"#FF00FF\",\"intensity\":\"high\"}"),
                ("Emerald Glow", "Rich emerald green glow", "effect", "text", 0.00m, 0, "💚", null, "{\"type\":\"emerald-glow\",\"color\":\"#50C878\",\"intensity\":\"medium\"}"),
                ("Coral Flow", "Smooth coral pink flow gradient", "effect", "text", 0.00m, 0, "🌸", null, "{\"type\":\"coral-flow\",\"colors\":[\"#FF7F50\",\"#FFB6C1\",\"#FFC0CB\"],\"speed\":\"slow\"}"),
                
                // Particle Effects Around Text - All Free
                ("Sparkle Particles", "Golden sparkles floating around text", "effect", "particle", 0.00m, 0, "✨", null, "{\"type\":\"sparkle-particles\",\"color\":\"#FFD700\",\"count\":20}"),
                ("Star Field", "Twinkling stars around text", "effect", "particle", 0.00m, 0, "⭐", null, "{\"type\":\"star-field\",\"color\":\"#FFFFFF\",\"count\":30}"),
                ("Floating Hearts", "Romantic hearts floating around", "effect", "particle", 0.00m, 0, "💖", null, "{\"type\":\"floating-hearts\",\"color\":\"#FF69B4\",\"count\":15}"),
                ("Fireflies", "Glowing fireflies around text", "effect", "particle", 0.00m, 0, "🪲", null, "{\"type\":\"fireflies\",\"color\":\"#FFFF00\",\"count\":25}"),
                ("Bubbles", "Colorful bubbles floating up", "effect", "particle", 0.00m, 0, "🫧", null, "{\"type\":\"bubbles\",\"colors\":[\"#FF6B9D\",\"#C44569\",\"#F8B500\"],\"count\":20}"),
                ("Confetti", "Celebration confetti particles", "effect", "particle", 0.00m, 0, "🎉", null, "{\"type\":\"confetti\",\"colors\":[\"#FF0000\",\"#00FF00\",\"#0000FF\",\"#FFFF00\"],\"count\":40}"),
                ("Snowflakes", "Gentle snowflakes falling", "effect", "particle", 0.00m, 0, "❄️", null, "{\"type\":\"snowflakes\",\"color\":\"#FFFFFF\",\"count\":30}"),
                ("Dust Particles", "Magical dust particles", "effect", "particle", 0.00m, 0, "✨", null, "{\"type\":\"dust-particles\",\"color\":\"#C0C0C0\",\"count\":35}"),
                ("Energy Orbs", "Glowing energy orbs around text", "effect", "particle", 0.00m, 0, "🔮", null, "{\"type\":\"energy-orbs\",\"colors\":[\"#FF00FF\",\"#00FFFF\"],\"count\":12}"),
                ("Floating Notes", "Musical notes floating around", "effect", "particle", 0.00m, 0, "🎵", null, "{\"type\":\"floating-notes\",\"color\":\"#9370DB\",\"count\":18}"),
                ("Rainbow Dots", "Colorful dots orbiting text", "effect", "particle", 0.00m, 0, "🎨", null, "{\"type\":\"rainbow-dots\",\"colors\":[\"#FF0000\",\"#FF7F00\",\"#FFFF00\",\"#00FF00\",\"#0000FF\",\"#4B0082\",\"#9400D3\"],\"count\":24}"),
                ("Glowing Rings", "Concentric glowing rings", "effect", "particle", 0.00m, 0, "⭕", null, "{\"type\":\"glowing-rings\",\"color\":\"#00FFFF\",\"count\":8}"),
                ("Magic Sparkles", "Magical sparkle trail effect", "effect", "particle", 0.00m, 0, "✨", null, "{\"type\":\"magic-sparkles\",\"color\":\"#FFD700\",\"count\":28}"),
                ("Floating Emojis", "Random emojis floating around", "effect", "particle", 0.00m, 0, "😊", null, "{\"type\":\"floating-emojis\",\"emojis\":[\"✨\",\"⭐\",\"💫\",\"🌟\"],\"count\":16}"),
                ("Colorful Dots", "Vibrant colored dots around text", "effect", "particle", 0.00m, 0, "🔴", null, "{\"type\":\"colorful-dots\",\"colors\":[\"#FF0000\",\"#00FF00\",\"#0000FF\",\"#FFFF00\"],\"count\":22}")
            };

            foreach (var product in products)
            {
                var insertSql = @"
                    INSERT INTO `products` (`name`, `description`, `type`, `category`, `price`, `premium_only`, `icon`, `preview_image`, `effect_data`, `is_active`)
                    VALUES (@name, @desc, @type, @category, @price, @premium, @icon, @preview, @effectData, 1)";

                using var command = new MySqlCommand(insertSql, connection);
                command.Parameters.AddWithValue("@name", product.name);
                command.Parameters.AddWithValue("@desc", product.desc);
                command.Parameters.AddWithValue("@type", product.type);
                command.Parameters.AddWithValue("@category", product.category);
                command.Parameters.AddWithValue("@price", product.price);
                command.Parameters.AddWithValue("@premium", product.premium);
                command.Parameters.AddWithValue("@icon", product.icon);
                command.Parameters.AddWithValue("@preview", product.preview ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@effectData", product.effectData);
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not insert product {product.name}: {ex.Message}");
                }
            }
        }

        private async Task CreateIndexesAsync(MySqlConnection connection)
        {
            var indexes = new[]
            {
                ("IDX_Users_Username", "CREATE INDEX `IDX_Users_Username` ON `users` (`Username`)"),
                ("IDX_Users_Email", "CREATE INDEX `IDX_Users_Email` ON `users` (`Email`)"),
                ("IDX_Users_DiscordId", "CREATE INDEX `IDX_Users_DiscordId` ON `users` (`DiscordId`)"),
                ("IDX_Style_Username", "CREATE INDEX `IDX_Style_Username` ON `style` (`username`)"),
                ("IDX_UserStyles_UserId", "CREATE INDEX `IDX_UserStyles_UserId` ON `user_styles` (`idUser`)")
            };

            foreach (var (indexName, indexSql) in indexes)
            {
                try
                {
                    // Kiểm tra xem index đã tồn tại chưa
                    var checkSql = $@"
                        SELECT COUNT(*) 
                        FROM information_schema.statistics 
                        WHERE table_schema = DATABASE() 
                        AND table_name IN ('users', 'style', 'user_styles')
                        AND index_name = '{indexName}'";

                    using var checkCommand = new MySqlCommand(checkSql, connection);
                    var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                    if (!exists)
                    {
                        using var command = new MySqlCommand(indexSql, connection);
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"Created index: {indexName}");
                    }
                }
                catch (MySqlException ex)
                {
                    // Index có thể đã tồn tại hoặc có lỗi khác
                    if (ex.Number == 1061) // Duplicate key name
                    {
                        Console.WriteLine($"Index {indexName} already exists, skipping...");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Could not create index {indexName}: {ex.Message}");
                    }
                }
            }
        }
    }
}

