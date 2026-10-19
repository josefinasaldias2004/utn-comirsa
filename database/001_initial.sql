-- SQL Server 2022+. Esquema inicial equivalente al modelo EF Core.
-- Ejecutar en una base Conecta vacía. El comando --init-db es la alternativa automática.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE TABLE dbo.Members (
 Id uniqueidentifier NOT NULL CONSTRAINT PK_Members PRIMARY KEY,
 Name nvarchar(120) NOT NULL,
 Email nvarchar(254) NOT NULL,
 PasswordHash nvarchar(512) NOT NULL,
 Organization nvarchar(20) NOT NULL,
 Approved bit NOT NULL,
 IsAdmin bit NOT NULL,
 FailedLogins int NOT NULL,
 LockedUntil datetimeoffset NULL,
 CreatedAt datetimeoffset NOT NULL
);
CREATE UNIQUE INDEX IX_Members_Email ON dbo.Members(Email);
CREATE TABLE dbo.Listings (
 Id uniqueidentifier NOT NULL CONSTRAINT PK_Listings PRIMARY KEY,
 OwnerId uniqueidentifier NOT NULL,
 Title nvarchar(100) NOT NULL,
 Description nvarchar(3000) NOT NULL,
 Type nvarchar(16) NOT NULL,
 Category nvarchar(30) NOT NULL,
 Price decimal(18,2) NULL,
 Contact nvarchar(254) NOT NULL,
 Image varbinary(max) NULL,
 ImageMime nvarchar(32) NULL,
 CreatedAt datetimeoffset NOT NULL,
 UpdatedAt datetimeoffset NOT NULL,
 CONSTRAINT FK_Listings_Members_OwnerId FOREIGN KEY (OwnerId) REFERENCES dbo.Members(Id)
);
CREATE INDEX IX_Listings_OwnerId ON dbo.Listings(OwnerId);
CREATE INDEX IX_Listings_CreatedAt ON dbo.Listings(CreatedAt);
COMMIT;
