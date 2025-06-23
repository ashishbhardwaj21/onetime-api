-- Initial migration for OneTime Dating App
-- Creates all necessary tables and relationships

-- Users table (extends ASP.NET Identity)
CREATE TABLE [dbo].[Users] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(256) NULL,
    [NormalizedUserName] NVARCHAR(256) NULL,
    [Email] NVARCHAR(256) NULL,
    [NormalizedEmail] NVARCHAR(256) NULL,
    [EmailConfirmed] BIT NOT NULL DEFAULT 0,
    [PasswordHash] NVARCHAR(MAX) NULL,
    [SecurityStamp] NVARCHAR(MAX) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [PhoneNumber] NVARCHAR(MAX) NULL,
    [PhoneNumberConfirmed] BIT NOT NULL DEFAULT 0,
    [TwoFactorEnabled] BIT NOT NULL DEFAULT 0,
    [LockoutEnd] DATETIMEOFFSET(7) NULL,
    [LockoutEnabled] BIT NOT NULL DEFAULT 0,
    [AccessFailedCount] INT NOT NULL DEFAULT 0,
    
    -- Custom fields
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [DateOfBirth] DATETIME2 NOT NULL,
    [Gender] NVARCHAR(20) NOT NULL,
    [Bio] NVARCHAR(500) NULL,
    [Occupation] NVARCHAR(100) NULL,
    [Education] NVARCHAR(100) NULL,
    [Height] INT NULL,
    [Drinking] NVARCHAR(50) NULL,
    [Smoking] NVARCHAR(50) NULL,
    [Children] NVARCHAR(50) NULL,
    [Religion] NVARCHAR(50) NULL,
    [PoliticalViews] NVARCHAR(50) NULL,
    [Latitude] FLOAT NULL,
    [Longitude] FLOAT NULL,
    [City] NVARCHAR(100) NULL,
    [Country] NVARCHAR(100) NULL,
    [IsVerified] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsBlocked] BIT NOT NULL DEFAULT 0,
    [IsPremium] BIT NOT NULL DEFAULT 0,
    [ShowMeOnDiscovery] BIT NOT NULL DEFAULT 1,
    [LastActive] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LikesRemaining] INT NOT NULL DEFAULT 50,
    [SuperLikesRemaining] INT NOT NULL DEFAULT 5,
    [BoostsRemaining] INT NOT NULL DEFAULT 0,
    [RewindsRemaining] INT NOT NULL DEFAULT 0,
    [LikesResetDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    [DeletedAt] DATETIME2 NULL
);

-- User Profiles table
CREATE TABLE [dbo].[UserProfiles] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [FullName] AS ([FirstName] + ' ' + [LastName]) PERSISTED,
    [CompletionPercentage] INT NOT NULL DEFAULT 0,
    [ViewCount] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    
    CONSTRAINT [FK_UserProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Photos table
CREATE TABLE [dbo].[Photos] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Url] NVARCHAR(500) NOT NULL,
    [ThumbnailUrl] NVARCHAR(500) NULL,
    [Order] INT NOT NULL DEFAULT 0,
    [IsMain] BIT NOT NULL DEFAULT 0,
    [IsVerificationPhoto] BIT NOT NULL DEFAULT 0,
    [IsApproved] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_Photos_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Interests table
CREATE TABLE [dbo].[Interests] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Category] NVARCHAR(50) NOT NULL,
    [Icon] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- User Interests table (many-to-many)
CREATE TABLE [dbo].[UserInterests] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [InterestId] NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_UserInterests_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserInterests_Interests] FOREIGN KEY ([InterestId]) REFERENCES [Interests] ([Id]) ON DELETE CASCADE
);

-- Matching Preferences table
CREATE TABLE [dbo].[MatchingPreferences] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [MinAge] INT NOT NULL DEFAULT 18,
    [MaxAge] INT NOT NULL DEFAULT 99,
    [MaxDistance] INT NOT NULL DEFAULT 50,
    [InterestedIn] NVARCHAR(20) NOT NULL DEFAULT 'Everyone',
    [OnlyVerifiedProfiles] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    
    CONSTRAINT [FK_MatchingPreferences_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Likes table
CREATE TABLE [dbo].[Likes] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [LikerId] NVARCHAR(450) NOT NULL,
    [LikedId] NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_Likes_Liker] FOREIGN KEY ([LikerId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Likes_Liked] FOREIGN KEY ([LikedId]) REFERENCES [Users] ([Id])
);

-- Super Likes table
CREATE TABLE [dbo].[SuperLikes] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [SuperLikerId] NVARCHAR(450) NOT NULL,
    [SuperLikedId] NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_SuperLikes_SuperLiker] FOREIGN KEY ([SuperLikerId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_SuperLikes_SuperLiked] FOREIGN KEY ([SuperLikedId]) REFERENCES [Users] ([Id])
);

-- Passes table
CREATE TABLE [dbo].[Passes] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [PasserId] NVARCHAR(450) NOT NULL,
    [PassedId] NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_Passes_Passer] FOREIGN KEY ([PasserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Passes_Passed] FOREIGN KEY ([PassedId]) REFERENCES [Users] ([Id])
);

-- Blocks table
CREATE TABLE [dbo].[Blocks] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [BlockerId] NVARCHAR(450) NOT NULL,
    [BlockedId] NVARCHAR(450) NOT NULL,
    [Reason] NVARCHAR(200) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_Blocks_Blocker] FOREIGN KEY ([BlockerId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Blocks_Blocked] FOREIGN KEY ([BlockedId]) REFERENCES [Users] ([Id])
);

-- Reports table
CREATE TABLE [dbo].[Reports] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [ReporterId] NVARCHAR(450) NOT NULL,
    [ReportedId] NVARCHAR(450) NOT NULL,
    [Reason] NVARCHAR(100) NOT NULL,
    [Details] NVARCHAR(1000) NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ResolvedAt] DATETIME2 NULL,
    
    CONSTRAINT [FK_Reports_Reporter] FOREIGN KEY ([ReporterId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Reports_Reported] FOREIGN KEY ([ReportedId]) REFERENCES [Users] ([Id])
);

-- Matches table
CREATE TABLE [dbo].[Matches] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [User1Id] NVARCHAR(450) NOT NULL,
    [User2Id] NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsSuperLikeMatch] BIT NOT NULL DEFAULT 0,
    [UnmatchedAt] DATETIME2 NULL,
    [UnmatchedById] NVARCHAR(450) NULL,
    
    CONSTRAINT [FK_Matches_User1] FOREIGN KEY ([User1Id]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Matches_User2] FOREIGN KEY ([User2Id]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Matches_UnmatchedBy] FOREIGN KEY ([UnmatchedById]) REFERENCES [Users] ([Id])
);

-- Conversations table
CREATE TABLE [dbo].[Conversations] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [MatchId] NVARCHAR(450) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsActive] BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT [FK_Conversations_Matches] FOREIGN KEY ([MatchId]) REFERENCES [Matches] ([Id]) ON DELETE CASCADE
);

-- Messages table
CREATE TABLE [dbo].[Messages] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [ConversationId] NVARCHAR(450) NOT NULL,
    [SenderId] NVARCHAR(450) NOT NULL,
    [Content] NVARCHAR(2000) NULL,
    [Type] NVARCHAR(50) NOT NULL DEFAULT 'text',
    [MediaUrl] NVARCHAR(500) NULL,
    [ThumbnailUrl] NVARCHAR(500) NULL,
    [Duration] INT NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    
    CONSTRAINT [FK_Messages_Conversations] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Messages_Sender] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id])
);

-- Message Reads table
CREATE TABLE [dbo].[MessageReads] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [MessageId] NVARCHAR(450) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [ReadAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_MessageReads_Messages] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MessageReads_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

-- Message Reactions table
CREATE TABLE [dbo].[MessageReactions] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [MessageId] NVARCHAR(450) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [Reaction] NVARCHAR(50) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_MessageReactions_Messages] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MessageReactions_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

-- Gamification Profiles table
CREATE TABLE [dbo].[GamificationProfiles] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Level] INT NOT NULL DEFAULT 1,
    [TotalXP] INT NOT NULL DEFAULT 0,
    [WeeklyXP] INT NOT NULL DEFAULT 0,
    [MonthlyXP] INT NOT NULL DEFAULT 0,
    [DailyLoginStreak] INT NOT NULL DEFAULT 0,
    [MessagingStreak] INT NOT NULL DEFAULT 0,
    [LastLoginDate] DATETIME2 NULL,
    [LastActivityDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    
    CONSTRAINT [FK_GamificationProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Badges table
CREATE TABLE [dbo].[Badges] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [Icon] NVARCHAR(100) NULL,
    [Rarity] NVARCHAR(50) NOT NULL DEFAULT 'Common',
    [Category] NVARCHAR(50) NOT NULL,
    [Requirement] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- User Badges table (many-to-many)
CREATE TABLE [dbo].[UserBadges] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [BadgeId] NVARCHAR(450) NOT NULL,
    [EarnedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_UserBadges_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserBadges_Badges] FOREIGN KEY ([BadgeId]) REFERENCES [Badges] ([Id]) ON DELETE CASCADE
);

-- Experience History table
CREATE TABLE [dbo].[ExperienceHistory] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Activity] NVARCHAR(100) NOT NULL,
    [XPEarned] INT NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_ExperienceHistory_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Subscriptions table
CREATE TABLE [dbo].[Subscriptions] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [PlanId] NVARCHAR(100) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL,
    [StartDate] DATETIME2 NOT NULL,
    [EndDate] DATETIME2 NULL,
    [Amount] DECIMAL(10,2) NOT NULL,
    [Currency] NVARCHAR(3) NOT NULL DEFAULT 'USD',
    [PaymentMethodId] NVARCHAR(200) NULL,
    [StripeSubscriptionId] NVARCHAR(200) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    
    CONSTRAINT [FK_Subscriptions_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Notifications table
CREATE TABLE [dbo].[Notifications] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Type] NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Body] NVARCHAR(1000) NOT NULL,
    [Data] NVARCHAR(MAX) NULL,
    [IsRead] BIT NOT NULL DEFAULT 0,
    [ReadAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Device Tokens table
CREATE TABLE [dbo].[DeviceTokens] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Token] NVARCHAR(500) NOT NULL,
    [Platform] NVARCHAR(20) NOT NULL,
    [DeviceModel] NVARCHAR(100) NULL,
    [AppVersion] NVARCHAR(20) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    
    CONSTRAINT [FK_DeviceTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);
CREATE INDEX [IX_Users_Gender] ON [Users] ([Gender]);
CREATE INDEX [IX_Users_DateOfBirth] ON [Users] ([DateOfBirth]);
CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);
CREATE INDEX [IX_Users_Location] ON [Users] ([Latitude], [Longitude]);
CREATE INDEX [IX_Users_LastActive] ON [Users] ([LastActive]);

CREATE INDEX [IX_Photos_UserId] ON [Photos] ([UserId]);
CREATE INDEX [IX_Photos_IsMain] ON [Photos] ([IsMain]);

CREATE INDEX [IX_UserInterests_UserId] ON [UserInterests] ([UserId]);
CREATE INDEX [IX_UserInterests_InterestId] ON [UserInterests] ([InterestId]);

CREATE INDEX [IX_Likes_LikerId] ON [Likes] ([LikerId]);
CREATE INDEX [IX_Likes_LikedId] ON [Likes] ([LikedId]);
CREATE INDEX [IX_Likes_CreatedAt] ON [Likes] ([CreatedAt]);

CREATE INDEX [IX_SuperLikes_SuperLikerId] ON [SuperLikes] ([SuperLikerId]);
CREATE INDEX [IX_SuperLikes_SuperLikedId] ON [SuperLikes] ([SuperLikedId]);

CREATE INDEX [IX_Passes_PasserId] ON [Passes] ([PasserId]);
CREATE INDEX [IX_Passes_PassedId] ON [Passes] ([PassedId]);

CREATE INDEX [IX_Matches_User1Id] ON [Matches] ([User1Id]);
CREATE INDEX [IX_Matches_User2Id] ON [Matches] ([User2Id]);
CREATE INDEX [IX_Matches_IsActive] ON [Matches] ([IsActive]);
CREATE INDEX [IX_Matches_CreatedAt] ON [Matches] ([CreatedAt]);

CREATE INDEX [IX_Messages_ConversationId] ON [Messages] ([ConversationId]);
CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
CREATE INDEX [IX_Messages_CreatedAt] ON [Messages] ([CreatedAt]);

CREATE INDEX [IX_GamificationProfiles_UserId] ON [GamificationProfiles] ([UserId]);
CREATE INDEX [IX_GamificationProfiles_Level] ON [GamificationProfiles] ([Level]);
CREATE INDEX [IX_GamificationProfiles_TotalXP] ON [GamificationProfiles] ([TotalXP]);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
CREATE INDEX [IX_Notifications_IsRead] ON [Notifications] ([IsRead]);
CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]);

-- Add unique constraints
ALTER TABLE [Photos] ADD CONSTRAINT [UQ_Photos_UserMain] UNIQUE ([UserId], [IsMain]);
ALTER TABLE [UserInterests] ADD CONSTRAINT [UQ_UserInterests] UNIQUE ([UserId], [InterestId]);
ALTER TABLE [Likes] ADD CONSTRAINT [UQ_Likes] UNIQUE ([LikerId], [LikedId]);
ALTER TABLE [SuperLikes] ADD CONSTRAINT [UQ_SuperLikes] UNIQUE ([SuperLikerId], [SuperLikedId]);
ALTER TABLE [Passes] ADD CONSTRAINT [UQ_Passes] UNIQUE ([PasserId], [PassedId]);
ALTER TABLE [Blocks] ADD CONSTRAINT [UQ_Blocks] UNIQUE ([BlockerId], [BlockedId]);
ALTER TABLE [UserBadges] ADD CONSTRAINT [UQ_UserBadges] UNIQUE ([UserId], [BadgeId]);
ALTER TABLE [DeviceTokens] ADD CONSTRAINT [UQ_DeviceTokens] UNIQUE ([UserId], [Token]);

-- Insert default interests
INSERT INTO [Interests] ([Id], [Name], [Category], [Icon]) VALUES
(NEWID(), 'Travel', 'Lifestyle', '✈️'),
(NEWID(), 'Photography', 'Creative', '📸'),
(NEWID(), 'Music', 'Entertainment', '🎵'),
(NEWID(), 'Fitness', 'Health', '💪'),
(NEWID(), 'Cooking', 'Lifestyle', '👨‍🍳'),
(NEWID(), 'Reading', 'Education', '📚'),
(NEWID(), 'Movies', 'Entertainment', '🎬'),
(NEWID(), 'Gaming', 'Entertainment', '🎮'),
(NEWID(), 'Art', 'Creative', '🎨'),
(NEWID(), 'Sports', 'Health', '⚽'),
(NEWID(), 'Dancing', 'Entertainment', '💃'),
(NEWID(), 'Yoga', 'Health', '🧘‍♀️'),
(NEWID(), 'Technology', 'Education', '💻'),
(NEWID(), 'Nature', 'Lifestyle', '🌲'),
(NEWID(), 'Coffee', 'Lifestyle', '☕'),
(NEWID(), 'Wine', 'Lifestyle', '🍷'),
(NEWID(), 'Fashion', 'Lifestyle', '👗'),
(NEWID(), 'Pets', 'Lifestyle', '🐕'),
(NEWID(), 'Meditation', 'Health', '🧘'),
(NEWID(), 'Hiking', 'Health', '🥾');

-- Insert default badges
INSERT INTO [Badges] ([Id], [Name], [Description], [Icon], [Rarity], [Category], [Requirement]) VALUES
(NEWID(), 'First Match', 'Got your first match!', '💕', 'Common', 'Matching', 'Get 1 match'),
(NEWID(), 'Social Butterfly', 'Matched with 10 people', '🦋', 'Rare', 'Matching', 'Get 10 matches'),
(NEWID(), 'Conversation Starter', 'Sent your first message', '💬', 'Common', 'Messaging', 'Send 1 message'),
(NEWID(), 'Chatterbox', 'Sent 100 messages', '📱', 'Epic', 'Messaging', 'Send 100 messages'),
(NEWID(), 'Super Star', 'Used your first Super Like', '⭐', 'Common', 'Matching', 'Use 1 Super Like'),
(NEWID(), 'Early Bird', 'Joined OneTime!', '🐦', 'Common', 'General', 'Sign up'),
(NEWID(), 'Verified', 'Completed profile verification', '✅', 'Rare', 'Profile', 'Complete verification'),
(NEWID(), 'Complete Profile', 'Added all profile information', '📋', 'Common', 'Profile', '100% profile completion'),
(NEWID(), 'Photo Perfect', 'Added 5 photos to profile', '📷', 'Common', 'Profile', 'Upload 5 photos'),
(NEWID(), 'Weekly Warrior', 'Active for 7 days straight', '🔥', 'Rare', 'Activity', '7 day login streak');