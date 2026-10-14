CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE TABLE `Organizations` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(255) CHARACTER SET utf8mb4 NULL,
        `PhoneNumber` varchar(255) CHARACTER SET utf8mb4 NULL,
        `Email` varchar(255) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_Organizations` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE TABLE `ChatBots` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `OrganizationId` int NOT NULL,
        `Name` varchar(255) CHARACTER SET utf8mb4 NULL,
        `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
        `SystemPromt` varchar(255) CHARACTER SET utf8mb4 NULL,
        `ApiKey` varchar(255) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_ChatBots` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ChatBots_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE TABLE `Analysis` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ChatBotId` int NOT NULL,
        `TotalQueries` int NOT NULL,
        `NumberOfDocuments` int NOT NULL,
        `TotalChunks` int NOT NULL,
        CONSTRAINT `PK_Analysis` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Analysis_ChatBots_ChatBotId` FOREIGN KEY (`ChatBotId`) REFERENCES `ChatBots` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE TABLE `Document` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ChatBotId` int NOT NULL,
        `Name` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Size` float NOT NULL,
        `UploadDate` date NOT NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NULL,
        `Chunks` int NOT NULL,
        `FileUrl` varchar(255) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_Document` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Document_ChatBots_ChatBotId` FOREIGN KEY (`ChatBotId`) REFERENCES `ChatBots` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE TABLE `LlmConfig` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ChatBotId` int NOT NULL,
        `ModelName` int NOT NULL,
        `Temperature` double NOT NULL,
        `MaxToken` int NOT NULL,
        `TopP` double NOT NULL,
        CONSTRAINT `PK_LlmConfig` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_LlmConfig_ChatBots_ChatBotId` FOREIGN KEY (`ChatBotId`) REFERENCES `ChatBots` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Analysis_ChatBotId` ON `Analysis` (`ChatBotId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE INDEX `IX_ChatBots_OrganizationId` ON `ChatBots` (`OrganizationId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE INDEX `IX_Document_ChatBotId` ON `Document` (`ChatBotId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_LlmConfig_ChatBotId` ON `LlmConfig` (`ChatBotId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261013134851_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261013134851_InitialCreate', '9.0.9');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

