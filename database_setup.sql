-- ========================================
-- Expense Tracker Database Setup Script
-- ========================================
-- Run this script in MySQL to set up the database manually
-- Create the database
DROP DATABASE IF EXISTS ExpenseTrackerDB;
CREATE DATABASE ExpenseTrackerDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- Use the database
USE ExpenseTrackerDB;
-- Create application user (optional)
DROP USER IF EXISTS 'expenseapp' @'localhost';
CREATE USER 'expenseapp' @'localhost' IDENTIFIED BY 'ExpenseApp123!';
GRANT ALL PRIVILEGES ON ExpenseTrackerDB.* TO 'expenseapp' @'localhost';
FLUSH PRIVILEGES;
-- Create Categories table
CREATE TABLE Categories (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    Color VARCHAR(7) NULL,
    CreatedDate DATETIME(6) NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    INDEX IX_Categories_Name (Name)
);
-- Insert default categories
INSERT INTO Categories (Name, Description, Color, CreatedDate, IsActive)
VALUES (
        'Food & Dining',
        'Restaurants, groceries, snacks',
        '#FF6B6B',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Transportation',
        'Gas, public transport, taxi',
        '#4ECDC4',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Shopping',
        'Clothing, electronics, gifts',
        '#45B7D1',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Entertainment',
        'Movies, games, hobbies',
        '#F9CA24',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Bills & Utilities',
        'Rent, electricity, internet',
        '#F0932B',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Healthcare',
        'Medical, pharmacy, insurance',
        '#EB4D4B',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Education',
        'Books, courses, training',
        '#6C5CE7',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Travel',
        'Vacation, business trips',
        '#A29BFE',
        UTC_TIMESTAMP(),
        1
    ),
    (
        'Other',
        'Miscellaneous expenses',
        '#74B9FF',
        UTC_TIMESTAMP(),
        1
    );
-- Create AspNetRoles table
CREATE TABLE AspNetRoles (
    Id VARCHAR(255) NOT NULL,
    Name VARCHAR(256) NULL,
    NormalizedName VARCHAR(256) NULL,
    ConcurrencyStamp LONGTEXT NULL,
    PRIMARY KEY (Id),
    UNIQUE INDEX RoleNameIndex (NormalizedName)
);
-- Insert default roles
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
VALUES (UUID(), 'Admin', 'ADMIN', UUID()),
    (UUID(), 'Manager', 'MANAGER', UUID()),
    (UUID(), 'Employee', 'EMPLOYEE', UUID());
-- Create AspNetUsers table
CREATE TABLE AspNetUsers (
    Id VARCHAR(255) NOT NULL,
    UserName VARCHAR(256) NULL,
    NormalizedUserName VARCHAR(256) NULL,
    Email VARCHAR(256) NULL,
    NormalizedEmail VARCHAR(256) NULL,
    EmailConfirmed TINYINT(1) NOT NULL,
    PasswordHash LONGTEXT NULL,
    SecurityStamp LONGTEXT NULL,
    ConcurrencyStamp LONGTEXT NULL,
    PhoneNumber LONGTEXT NULL,
    PhoneNumberConfirmed TINYINT(1) NOT NULL,
    TwoFactorEnabled TINYINT(1) NOT NULL,
    LockoutEnd DATETIME(6) NULL,
    LockoutEnabled TINYINT(1) NOT NULL,
    AccessFailedCount INT NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    CreatedDate DATETIME(6) NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE INDEX UserNameIndex (NormalizedUserName),
    INDEX EmailIndex (NormalizedEmail)
);
-- Create AspNetUserRoles table
CREATE TABLE AspNetUserRoles (
    UserId VARCHAR(255) NOT NULL,
    RoleId VARCHAR(255) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    INDEX IX_AspNetUserRoles_RoleId (RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
-- Create AspNetUserClaims table
CREATE TABLE AspNetUserClaims (
    Id INT NOT NULL AUTO_INCREMENT,
    UserId VARCHAR(255) NOT NULL,
    ClaimType LONGTEXT NULL,
    ClaimValue LONGTEXT NULL,
    PRIMARY KEY (Id),
    INDEX IX_AspNetUserClaims_UserId (UserId),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
-- Create AspNetUserLogins table
CREATE TABLE AspNetUserLogins (
    LoginProvider VARCHAR(255) NOT NULL,
    ProviderKey VARCHAR(255) NOT NULL,
    ProviderDisplayName LONGTEXT NULL,
    UserId VARCHAR(255) NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    INDEX IX_AspNetUserLogins_UserId (UserId),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
-- Create AspNetUserTokens table
CREATE TABLE AspNetUserTokens (
    UserId VARCHAR(255) NOT NULL,
    LoginProvider VARCHAR(255) NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Value LONGTEXT NULL,
    PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
-- Create AspNetRoleClaims table
CREATE TABLE AspNetRoleClaims (
    Id INT NOT NULL AUTO_INCREMENT,
    RoleId VARCHAR(255) NOT NULL,
    ClaimType LONGTEXT NULL,
    ClaimValue LONGTEXT NULL,
    PRIMARY KEY (Id),
    INDEX IX_AspNetRoleClaims_RoleId (RoleId),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);
-- Create Expenses table
CREATE TABLE Expenses (
    Id INT NOT NULL AUTO_INCREMENT,
    Title VARCHAR(200) NOT NULL,
    Description VARCHAR(1000) NULL,
    Amount DECIMAL(18, 2) NOT NULL,
    ExpenseDate DATETIME(6) NOT NULL,
    CreatedDate DATETIME(6) NOT NULL,
    ModifiedDate DATETIME(6) NULL,
    UserId VARCHAR(255) NOT NULL,
    CategoryId INT NOT NULL,
    PRIMARY KEY (Id),
    INDEX IX_Expenses_CategoryId (CategoryId),
    INDEX IX_Expenses_UserId (UserId),
    INDEX IX_Expenses_ExpenseDate (ExpenseDate),
    CONSTRAINT FK_Expenses_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Expenses_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES Categories (Id) ON DELETE RESTRICT
);
-- Create EFMigrationsHistory table (for Entity Framework)
CREATE TABLE __EFMigrationsHistory (
    MigrationId VARCHAR(150) NOT NULL,
    ProductVersion VARCHAR(32) NOT NULL,
    PRIMARY KEY (MigrationId)
);
-- Show created tables
SHOW TABLES;
-- Show table structures
DESCRIBE Categories;
DESCRIBE AspNetUsers;
DESCRIBE AspNetRoles;
DESCRIBE Expenses;
-- Display success message
SELECT 'Database setup completed successfully!' as Status;
SELECT 'Default categories inserted' as Categories;
SELECT 'Default roles inserted' as Roles;
SELECT 'Ready for Entity Framework migrations' as EF;