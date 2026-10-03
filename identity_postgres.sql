-- PostgreSQL script to create ASP.NET Core Identity tables
-- Adjust schema/table names as needed for your setup

CREATE TABLE "AspNetRoles" (
    "Id" VARCHAR(450) NOT NULL PRIMARY KEY,
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" VARCHAR(256)
);

CREATE TABLE "AspNetUsers" (
    "Id" VARCHAR(450) NOT NULL PRIMARY KEY,
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL,
    "PasswordHash" VARCHAR(256),
    "SecurityStamp" VARCHAR(256),
    "ConcurrencyStamp" VARCHAR(256),
    "PhoneNumber" VARCHAR(256),
    "PhoneNumberConfirmed" BOOLEAN NOT NULL,
    "TwoFactorEnabled" BOOLEAN NOT NULL,
    "LockoutEnd" TIMESTAMP,
    "LockoutEnabled" BOOLEAN NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" VARCHAR(450) NOT NULL,
    "RoleId" VARCHAR(450) NOT NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(450) NOT NULL,
    "ClaimType" VARCHAR(256),
    "ClaimValue" VARCHAR(256),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" VARCHAR(128) NOT NULL,
    "ProviderKey" VARCHAR(128) NOT NULL,
    "ProviderDisplayName" VARCHAR(256),
    "UserId" VARCHAR(450) NOT NULL,
    PRIMARY KEY ("LoginProvider", "ProviderKey"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" VARCHAR(450) NOT NULL,
    "ClaimType" VARCHAR(256),
    "ClaimValue" VARCHAR(256),
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" VARCHAR(450) NOT NULL,
    "LoginProvider" VARCHAR(128) NOT NULL,
    "Name" VARCHAR(128) NOT NULL,
    "Value" VARCHAR(256),
    PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);
