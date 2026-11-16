IF DB_ID(N'GarageHub') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [GarageHub]');
END;
GO

USE [GarageHub];
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(200) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [GoogleId] nvarchar(100) NULL,
        [Role] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (DATEADD(HOUR, 7, GETUTCDATE())),
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE TABLE [Garages] (
        [Id] int NOT NULL IDENTITY,
        [OwnerId] int NULL,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [AvgRating] decimal(3,2) NOT NULL,
        [CapacityPerSlot] int NOT NULL,
        CONSTRAINT [PK_Garages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Garages_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [Id] int NOT NULL IDENTITY,
        [OwnerId] int NOT NULL,
        [PlateNumber] nvarchar(20) NOT NULL,
        [Brand] nvarchar(100) NOT NULL,
        [Model] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (DATEADD(HOUR, 7, GETUTCDATE())),
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vehicles_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE TABLE [Reminders] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Type] nvarchar(100) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Reminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reminders_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE TABLE [ServiceBookings] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [GarageId] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ScheduledAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (DATEADD(HOUR, 7, GETUTCDATE())),
        [ExpiresAt] datetime2 NULL,
        CONSTRAINT [PK_ServiceBookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceBookings_Garages_GarageId] FOREIGN KEY ([GarageId]) REFERENCES [Garages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceBookings_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [PayOSTransactionId] nvarchar(100) NULL,
        [PaidAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_ServiceBookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [ServiceBookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (DATEADD(HOUR, 7, GETUTCDATE())),
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_ServiceBookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [ServiceBookings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'AvgRating', N'CapacityPerSlot', N'Name', N'OwnerId', N'Phone') AND [object_id] = OBJECT_ID(N'[Garages]'))
        SET IDENTITY_INSERT [Garages] ON;
    EXEC(N'INSERT INTO [Garages] ([Id], [Address], [AvgRating], [CapacityPerSlot], [Name], [OwnerId], [Phone])
    VALUES (1, N''1 Sample Street, District 1, Ho Chi Minh City'', 0.0, 2, N''Sample Garage 1'', NULL, N''0900000001''),
    (2, N''2 Sample Street, District 3, Ho Chi Minh City'', 0.0, 2, N''Sample Garage 2'', NULL, N''0900000002''),
    (3, N''3 Sample Street, Thu Duc City, Ho Chi Minh City'', 0.0, 2, N''Sample Garage 3'', NULL, N''0900000003'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'AvgRating', N'CapacityPerSlot', N'Name', N'OwnerId', N'Phone') AND [object_id] = OBJECT_ID(N'[Garages]'))
        SET IDENTITY_INSERT [Garages] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Garages_OwnerId] ON [Garages] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reminders_VehicleId] ON [Reminders] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reviews_BookingId] ON [Reviews] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceBookings_GarageId_ScheduledAt] ON [ServiceBookings] ([GarageId], [ScheduledAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceBookings_VehicleId] ON [ServiceBookings] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vehicles_OwnerId] ON [Vehicles] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vehicles_PlateNumber] ON [Vehicles] ([PlateNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112165603_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112165603_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

