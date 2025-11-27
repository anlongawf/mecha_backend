-- Mecha Backend Database Schema
-- MariaDB/MySQL Database Structure
-- Script này sẽ tự động xóa các bảng cũ (nếu có) và tạo lại toàn bộ schema

-- ============================================
-- BƯỚC 1: XÓA CÁC BẢNG CŨ (nếu có)
-- ============================================
-- Xóa theo thứ tự ngược để tránh lỗi foreign key constraint

SET FOREIGN_KEY_CHECKS = 0; -- Tắt kiểm tra foreign key tạm thời

DROP TABLE IF EXISTS `user_styles`;
DROP TABLE IF EXISTS `users`;
DROP TABLE IF EXISTS `style`;

SET FOREIGN_KEY_CHECKS = 1; -- Bật lại kiểm tra foreign key

-- ============================================
-- BƯỚC 2: TẠO CÁC BẢNG MỚI
-- ============================================

-- Table: style (phải tạo trước vì được tham chiếu bởi users)
CREATE TABLE `style` (
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table: users (tạo sau style vì có foreign key đến style)
CREATE TABLE `users` (
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table: user_styles (tạo sau users vì có foreign key đến users)
CREATE TABLE `user_styles` (
  `style_id` INT(11) NOT NULL AUTO_INCREMENT,
  `idUser` INT(11) NOT NULL,
  `styles` JSON DEFAULT NULL,
  PRIMARY KEY (`style_id`),
  UNIQUE KEY `UK_UserStyle` (`idUser`),
  CONSTRAINT `FK_UserStyle_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- BƯỚC 3: TẠO CÁC INDEXES ĐỂ TỐI ƯU HIỆU SUẤT
-- ============================================

-- Indexes cho bảng users
CREATE INDEX `IDX_Users_Username` ON `users` (`Username`);
CREATE INDEX `IDX_Users_Email` ON `users` (`Email`);
CREATE INDEX `IDX_Users_DiscordId` ON `users` (`DiscordId`);

-- Indexes cho bảng style
CREATE INDEX `IDX_Style_Username` ON `style` (`username`);

-- Indexes cho bảng user_styles
CREATE INDEX `IDX_UserStyles_UserId` ON `user_styles` (`idUser`);

-- ============================================
-- BƯỚC 4: TẠO BẢNG CHO SHOP VÀ EFFECTS
-- ============================================

-- Table: products (Shop items - effects, themes, etc.)
CREATE TABLE `products` (
  `product_id` INT(11) NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(255) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `type` VARCHAR(50) NOT NULL DEFAULT 'effect', -- 'effect', 'theme', 'premium'
  `category` VARCHAR(50) DEFAULT NULL, -- 'animation', 'particle', 'transition', 'cursor', etc.
  `price` DECIMAL(10, 2) DEFAULT 0.00,
  `premium_only` TINYINT(1) DEFAULT 0,
  `icon` VARCHAR(255) DEFAULT NULL,
  `preview_image` VARCHAR(255) DEFAULT NULL,
  `effect_data` JSON DEFAULT NULL, -- Store effect configuration
  `is_active` TINYINT(1) DEFAULT 1,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`product_id`),
  KEY `IDX_Products_Type` (`type`),
  KEY `IDX_Products_Category` (`category`),
  KEY `IDX_Products_Active` (`is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table: purchases (Purchase history)
CREATE TABLE `purchases` (
  `purchase_id` INT(11) NOT NULL AUTO_INCREMENT,
  `idUser` INT(11) NOT NULL,
  `product_id` INT(11) NOT NULL,
  `price` DECIMAL(10, 2) NOT NULL,
  `payment_method` VARCHAR(50) DEFAULT NULL,
  `transaction_id` VARCHAR(255) DEFAULT NULL,
  `status` VARCHAR(50) DEFAULT 'completed', -- 'pending', 'completed', 'failed', 'refunded'
  `purchased_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`purchase_id`),
  KEY `FK_Purchase_User` (`idUser`),
  KEY `FK_Purchase_Product` (`product_id`),
  CONSTRAINT `FK_Purchase_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_Purchase_Product` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table: user_effects (User owned effects/applied effects)
CREATE TABLE `user_effects` (
  `effect_id` INT(11) NOT NULL AUTO_INCREMENT,
  `idUser` INT(11) NOT NULL,
  `product_id` INT(11) NOT NULL,
  `is_active` TINYINT(1) DEFAULT 1,
  `applied_to` VARCHAR(50) DEFAULT 'profile', -- 'profile', 'background', 'cursor', 'avatar', etc.
  `effect_settings` JSON DEFAULT NULL, -- Custom settings for the effect
  `applied_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`effect_id`),
  KEY `FK_UserEffect_User` (`idUser`),
  KEY `FK_UserEffect_Product` (`product_id`),
  CONSTRAINT `FK_UserEffect_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_UserEffect_Product` FOREIGN KEY (`product_id`) REFERENCES `products` (`product_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table: user_wallet (Virtual currency/coins for purchases)
CREATE TABLE `user_wallet` (
  `wallet_id` INT(11) NOT NULL AUTO_INCREMENT,
  `idUser` INT(11) NOT NULL,
  `coins` INT(11) DEFAULT 0,
  `last_updated` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`wallet_id`),
  UNIQUE KEY `UK_UserWallet` (`idUser`),
  CONSTRAINT `FK_Wallet_User` FOREIGN KEY (`idUser`) REFERENCES `users` (`IdUser`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================
-- HOÀN TẤT!
-- ============================================
-- Schema đã được tạo thành công!

