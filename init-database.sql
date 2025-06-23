-- OneTime Dating App Database Schema
-- Run this script in Azure SQL Database Query Editor

-- Users table
CREATE TABLE Users (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL,
    TwoFactorEnabled BIT NOT NULL,
    LockoutEnd DATETIMEOFFSET(7) NULL,
    LockoutEnabled BIT NOT NULL,
    AccessFailedCount INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    DateOfBirth DATETIME2 NOT NULL,
    Gender NVARCHAR(20) NOT NULL,
    InterestedIn NVARCHAR(20) NOT NULL,
    Bio NVARCHAR(500) NULL,
    LocationLatitude DECIMAL(10,8) NULL,
    LocationLongitude DECIMAL(11,8) NULL,
    LocationCity NVARCHAR(100) NULL,
    LocationCountry NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsPremium BIT NOT NULL DEFAULT 0,
    PremiumExpiryDate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastActiveAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- User Photos table
CREATE TABLE UserPhotos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    PhotoUrl NVARCHAR(500) NOT NULL,
    IsMain BIT NOT NULL DEFAULT 0,
    IsVerified BIT NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- User Interests table
CREATE TABLE UserInterests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    InterestId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Interests table
CREATE TABLE Interests (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    Icon NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- User Likes table (swipe actions)
CREATE TABLE UserLikes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    LikedUserId NVARCHAR(450) NOT NULL,
    LikeType NVARCHAR(20) NOT NULL, -- 'Like', 'SuperLike', 'Pass'
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (LikedUserId) REFERENCES Users(Id),
    UNIQUE(UserId, LikedUserId)
);

-- Matches table
CREATE TABLE Matches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    User1Id NVARCHAR(450) NOT NULL,
    User2Id NVARCHAR(450) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ExpiresAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (User1Id) REFERENCES Users(Id),
    FOREIGN KEY (User2Id) REFERENCES Users(Id),
    UNIQUE(User1Id, User2Id)
);

-- Conversations table
CREATE TABLE Conversations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MatchId INT NOT NULL,
    LastMessageId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (MatchId) REFERENCES Matches(Id) ON DELETE CASCADE
);

-- Messages table
CREATE TABLE Messages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ConversationId INT NOT NULL,
    SenderId NVARCHAR(450) NOT NULL,
    Content NVARCHAR(2000) NULL,
    MessageType NVARCHAR(20) NOT NULL DEFAULT 'Text', -- 'Text', 'Photo', 'Voice', 'Video'
    MediaUrl NVARCHAR(500) NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    ReadAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE,
    FOREIGN KEY (SenderId) REFERENCES Users(Id)
);

-- User Blocks table
CREATE TABLE UserBlocks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    BlockedUserId NVARCHAR(450) NOT NULL,
    Reason NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (BlockedUserId) REFERENCES Users(Id),
    UNIQUE(UserId, BlockedUserId)
);

-- User Reports table
CREATE TABLE UserReports (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReporterId NVARCHAR(450) NOT NULL,
    ReportedUserId NVARCHAR(450) NOT NULL,
    Reason NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Resolved', 'Dismissed'
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ResolvedAt DATETIME2 NULL,
    FOREIGN KEY (ReporterId) REFERENCES Users(Id),
    FOREIGN KEY (ReportedUserId) REFERENCES Users(Id)
);

-- Gamification Profiles table
CREATE TABLE GamificationProfiles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL UNIQUE,
    XP INT NOT NULL DEFAULT 0,
    Level INT NOT NULL DEFAULT 1,
    Streak INT NOT NULL DEFAULT 0,
    LastActivityDate DATE NULL,
    TotalLikes INT NOT NULL DEFAULT 0,
    TotalMatches INT NOT NULL DEFAULT 0,
    TotalMessages INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- User Badges table
CREATE TABLE UserBadges (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    BadgeId INT NOT NULL,
    EarnedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Badges table
CREATE TABLE Badges (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(200) NOT NULL,
    Icon NVARCHAR(100) NOT NULL,
    Condition NVARCHAR(200) NOT NULL,
    XPReward INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Push Notifications table
CREATE TABLE PushNotifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Body NVARCHAR(500) NOT NULL,
    Data NVARCHAR(MAX) NULL,
    IsSent BIT NOT NULL DEFAULT 0,
    SentAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Device Tokens table (for push notifications)
CREATE TABLE DeviceTokens (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Token NVARCHAR(500) NOT NULL,
    Platform NVARCHAR(20) NOT NULL, -- 'iOS', 'Android'
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX IX_Users_Email ON Users(NormalizedEmail);
CREATE INDEX IX_Users_UserName ON Users(NormalizedUserName);
CREATE INDEX IX_Users_Location ON Users(LocationLatitude, LocationLongitude);
CREATE INDEX IX_Users_LastActiveAt ON Users(LastActiveAt);
CREATE INDEX IX_UserPhotos_UserId ON UserPhotos(UserId);
CREATE INDEX IX_UserLikes_UserId ON UserLikes(UserId);
CREATE INDEX IX_UserLikes_LikedUserId ON UserLikes(LikedUserId);
CREATE INDEX IX_Matches_User1Id ON Matches(User1Id);
CREATE INDEX IX_Matches_User2Id ON Matches(User2Id);
CREATE INDEX IX_Messages_ConversationId ON Messages(ConversationId);
CREATE INDEX IX_Messages_CreatedAt ON Messages(CreatedAt);

-- Insert initial interests
INSERT INTO Interests (Name, Category) VALUES 
('Photography', 'Creative'),
('Travel', 'Lifestyle'),
('Cooking', 'Lifestyle'),
('Music', 'Creative'),
('Sports', 'Activities'),
('Reading', 'Education'),
('Movies', 'Entertainment'),
('Gaming', 'Entertainment'),
('Fitness', 'Health'),
('Art', 'Creative'),
('Dancing', 'Activities'),
('Technology', 'Education'),
('Nature', 'Lifestyle'),
('Fashion', 'Lifestyle'),
('Food', 'Lifestyle');

-- Insert initial badges
INSERT INTO Badges (Name, Description, Icon, Condition, XPReward) VALUES 
('First Match', 'Got your first match!', '🎯', 'first_match', 100),
('Conversation Starter', 'Sent 10 messages', '💬', 'messages_10', 50),
('Social Butterfly', 'Got 50 matches', '🦋', 'matches_50', 200),
('Explorer', 'Liked 100 profiles', '🗺️', 'likes_100', 75),
('Loyal User', 'Used app for 30 days', '👑', 'days_30', 150),
('Photo Perfect', 'Added 5 photos', '📸', 'photos_5', 25),
('Super Liker', 'Used 10 super likes', '⭐', 'super_likes_10', 100);

PRINT 'Database schema created successfully!';
PRINT 'You can now deploy your API to Azure.';