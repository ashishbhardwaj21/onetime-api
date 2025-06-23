using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OneTime.API.Models;
using OneTime.API.Models.Entities;

namespace OneTime.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // User and Profile entities
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<UserInterest> UserInterests { get; set; }
    public DbSet<Interest> Interests { get; set; }
    public DbSet<UserVerification> UserVerifications { get; set; }
    public DbSet<UserLocation> UserLocations { get; set; }

    // Matching entities
    public DbSet<Match> Matches { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Pass> Passes { get; set; }
    public DbSet<SuperLike> SuperLikes { get; set; }
    public DbSet<Block> Blocks { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<MatchingPreference> MatchingPreferences { get; set; }

    // Messaging entities
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageReaction> MessageReactions { get; set; }
    public DbSet<MessageRead> MessageReads { get; set; }

    // Gamification entities
    public DbSet<GamificationProfile> GamificationProfiles { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<UserChallenge> UserChallenges { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<XPTransaction> XPTransactions { get; set; }

    // Subscription and Payment entities
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PremiumFeature> PremiumFeatures { get; set; }
    public DbSet<UserPremiumFeature> UserPremiumFeatures { get; set; }

    // Analytics entities
    public DbSet<UserActivity> UserActivities { get; set; }
    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    // Notification entities
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    // Time-based matching entities
    public DbSet<AvailabilityStatus> AvailabilityStatuses { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<QuickMeetup> QuickMeetups { get; set; }
    public DbSet<ScheduledDate> ScheduledDates { get; set; }

    // AI and ML entities
    public DbSet<AIMatchSuggestion> AIMatchSuggestions { get; set; }
    public DbSet<CompatibilityScore> CompatibilityScores { get; set; }
    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; }
    public DbSet<ConversationStarter> ConversationStarters { get; set; }

    // Moderation entities
    public DbSet<ContentModerationResult> ContentModerationResults { get; set; }
    public DbSet<ModerationAction> ModerationActions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure entity relationships and constraints
        ConfigureUserEntities(builder);
        ConfigureMatchingEntities(builder);
        ConfigureMessagingEntities(builder);
        ConfigureGamificationEntities(builder);
        ConfigureSubscriptionEntities(builder);
        ConfigureAnalyticsEntities(builder);
        ConfigureNotificationEntities(builder);
        ConfigureTimeBasedEntities(builder);
        ConfigureAIEntities(builder);
        ConfigureModerationEntities(builder);

        // Configure indexes for performance
        ConfigureIndexes(builder);

        // Seed initial data
        SeedData(builder);
    }

    private void ConfigureUserEntities(ModelBuilder builder)
    {
        // UserProfile configuration
        builder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<UserProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.JobTitle).HasMaxLength(100);
            entity.Property(e => e.Company).HasMaxLength(100);
            entity.Property(e => e.School).HasMaxLength(100);
        });

        // Photo configuration
        builder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.UserProfile)
                  .WithMany(e => e.Photos)
                  .HasForeignKey(e => e.UserProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => new { e.UserProfileId, e.Order });
        });

        // UserInterest many-to-many relationship
        builder.Entity<UserInterest>(entity =>
        {
            entity.HasKey(e => new { e.UserProfileId, e.InterestId });
            entity.HasOne(e => e.UserProfile)
                  .WithMany(e => e.UserInterests)
                  .HasForeignKey(e => e.UserProfileId);
            entity.HasOne(e => e.Interest)
                  .WithMany(e => e.UserInterests)
                  .HasForeignKey(e => e.InterestId);
        });

        // UserLocation configuration
        builder.Entity<UserLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            entity.HasIndex(e => e.UpdatedAt);
        });
    }

    private void ConfigureMatchingEntities(ModelBuilder builder)
    {
        // Match configuration
        builder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User1)
                  .WithMany()
                  .HasForeignKey(e => e.User1Id)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User2)
                  .WithMany()
                  .HasForeignKey(e => e.User2Id)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.User1Id, e.User2Id }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Like configuration
        builder.Entity<Like>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Liker)
                  .WithMany()
                  .HasForeignKey(e => e.LikerId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Liked)
                  .WithMany()
                  .HasForeignKey(e => e.LikedId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.LikerId, e.LikedId }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // MatchingPreference configuration
        builder.Entity<MatchingPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<MatchingPreference>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureMessagingEntities(ModelBuilder builder)
    {
        // Conversation configuration
        builder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Match)
                  .WithOne(e => e.Conversation)
                  .HasForeignKey<Conversation>(e => e.MatchId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.LastMessageAt);
        });

        // Message configuration
        builder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Conversation)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Sender)
                  .WithMany()
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Content).HasMaxLength(2000);
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
        });

        // MessageRead configuration
        builder.Entity<MessageRead>(entity =>
        {
            entity.HasKey(e => new { e.MessageId, e.UserId });
            entity.HasOne(e => e.Message)
                  .WithMany(e => e.MessageReads)
                  .HasForeignKey(e => e.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureGamificationEntities(ModelBuilder builder)
    {
        // GamificationProfile configuration
        builder.Entity<GamificationProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<GamificationProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TotalXP);
            entity.HasIndex(e => e.Level);
        });

        // UserBadge many-to-many relationship
        builder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BadgeId });
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Badge)
                  .WithMany()
                  .HasForeignKey(e => e.BadgeId);

            entity.HasIndex(e => e.EarnedAt);
        });

        // XPTransaction configuration
        builder.Entity<XPTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
        });
    }

    private void ConfigureSubscriptionEntities(ModelBuilder builder)
    {
        // Subscription configuration
        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.Status });
        });

        // Payment configuration
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.TransactionId).IsUnique();
        });
    }

    private void ConfigureAnalyticsEntities(ModelBuilder builder)
    {
        // AnalyticsEvent configuration
        builder.Entity<AnalyticsEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.EventName, e.Timestamp });
            entity.HasIndex(e => e.Timestamp);
        });

        // UserSession configuration
        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.StartTime });
        });
    }

    private void ConfigureNotificationEntities(ModelBuilder builder)
    {
        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
        });

        // DeviceToken configuration
        builder.Entity<DeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
        });
    }

    private void ConfigureTimeBasedEntities(ModelBuilder builder)
    {
        // AvailabilityStatus configuration
        builder.Entity<AvailabilityStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.Status, e.ExpiresAt });
        });

        // QuickMeetup configuration
        builder.Entity<QuickMeetup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.StartTime, e.EndTime });
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });
    }

    private void ConfigureAIEntities(ModelBuilder builder)
    {
        // CompatibilityScore configuration
        builder.Entity<CompatibilityScore>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.User1Id, e.User2Id }).IsUnique();
            entity.HasIndex(e => e.Score);
        });

        // PersonalityProfile configuration
        builder.Entity<PersonalityProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<PersonalityProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureModerationEntities(ModelBuilder builder)
    {
        // ContentModerationResult configuration
        builder.Entity<ContentModerationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ContentType, e.Status });
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private void ConfigureIndexes(ModelBuilder builder)
    {
        // Performance indexes
        builder.Entity<ApplicationUser>()
            .HasIndex(e => e.Email)
            .IsUnique();

        builder.Entity<ApplicationUser>()
            .HasIndex(e => e.PhoneNumber);

        builder.Entity<ApplicationUser>()
            .HasIndex(e => e.LastActive);
    }

    private void SeedData(ModelBuilder builder)
    {
        // Seed interests
        var interests = new List<Interest>
        {
            new() { Id = 1, Name = "Travel", Icon = "✈️", Category = "Lifestyle" },
            new() { Id = 2, Name = "Music", Icon = "🎵", Category = "Entertainment" },
            new() { Id = 3, Name = "Fitness", Icon = "💪", Category = "Health" },
            new() { Id = 4, Name = "Cooking", Icon = "👨‍🍳", Category = "Lifestyle" },
            new() { Id = 5, Name = "Photography", Icon = "📷", Category = "Art" },
            new() { Id = 6, Name = "Reading", Icon = "📚", Category = "Education" },
            new() { Id = 7, Name = "Movies", Icon = "🎬", Category = "Entertainment" },
            new() { Id = 8, Name = "Gaming", Icon = "🎮", Category = "Entertainment" },
            new() { Id = 9, Name = "Hiking", Icon = "🥾", Category = "Outdoor" },
            new() { Id = 10, Name = "Art", Icon = "🎨", Category = "Art" }
        };

        builder.Entity<Interest>().HasData(interests);

        // Seed badges
        var badges = new List<Badge>
        {
            new() { Id = 1, Name = "First Match", Description = "Got your first match!", Icon = "🎯", XPReward = 50, Category = "Milestone" },
            new() { Id = 2, Name = "Conversation Starter", Description = "Sent your first message", Icon = "💬", XPReward = 25, Category = "Communication" },
            new() { Id = 3, Name = "Profile Complete", Description = "Completed your profile", Icon = "✅", XPReward = 100, Category = "Profile" },
            new() { Id = 4, Name = "Photo Verified", Description = "Verified your photos", Icon = "📸", XPReward = 75, Category = "Verification" },
            new() { Id = 5, Name = "Week Warrior", Description = "Active for 7 consecutive days", Icon = "🔥", XPReward = 150, Category = "Engagement" }
        };

        builder.Entity<Badge>().HasData(badges);

        // Seed notification templates
        var notificationTemplates = new List<NotificationTemplate>
        {
            new() { Id = 1, Name = "NewMatch", Title = "It's a Match! 🎉", Body = "You and {userName} liked each other!", Type = "Match" },
            new() { Id = 2, Name = "NewMessage", Title = "New Message", Body = "{userName} sent you a message", Type = "Message" },
            new() { Id = 3, Name = "ProfileView", Title = "Someone viewed your profile", Body = "Your profile is getting attention!", Type = "Profile" },
            new() { Id = 4, Name = "SuperLike", Title = "You received a Super Like! ⭐", Body = "{userName} super liked you!", Type = "Like" }
        };

        builder.Entity<NotificationTemplate>().HasData(notificationTemplates);
    }
}