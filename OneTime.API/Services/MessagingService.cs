using Microsoft.EntityFrameworkCore;
using OneTime.API.Data;
using OneTime.API.Models;
using OneTime.API.Models.DTOs;
using OneTime.API.Models.Entities;

namespace OneTime.API.Services;

public interface IMessagingService
{
    Task<ServiceResult<List<ConversationResponse>>> GetConversationsAsync(string userId);
    Task<ServiceResult<PaginatedResponse<MessageResponse>>> GetMessagesAsync(string userId, string conversationId, int page = 1, int pageSize = 50);
    Task<ServiceResult<MessageResponse>> SendMessageAsync(string userId, SendMessageRequest request);
    Task<ServiceResult<MessageResponse>> UpdateMessageAsync(string userId, string messageId, string newContent);
    Task<ServiceResult<bool>> DeleteMessageAsync(string userId, string messageId);
    Task<ServiceResult<bool>> MarkMessageAsReadAsync(string userId, string messageId);
    Task<ServiceResult<bool>> MarkConversationAsReadAsync(string userId, string conversationId);
    Task<ServiceResult<bool>> AddMessageReactionAsync(string userId, string messageId, string reaction);
    Task<ServiceResult<bool>> RemoveMessageReactionAsync(string userId, string messageId, string reaction);
    Task<ServiceResult<MediaUploadResponse>> UploadMessageMediaAsync(string userId, UploadMediaRequest request);
    Task<ServiceResult<List<MediaResponse>>> GetConversationMediaAsync(string userId, string conversationId, string type);
    Task<ServiceResult<bool>> TrackTypingActivityAsync(string userId, string conversationId, bool isTyping);
    Task<ServiceResult<int>> GetUnreadMessageCountAsync(string userId);
    Task<ServiceResult<bool>> UpdateUserOnlineStatusAsync(string userId, bool isOnline);
    Task<bool> UserHasAccessToConversationAsync(string userId, string conversationId);
    Task<string?> GetConversationIdForMessageAsync(string messageId);
    Task SendMessageNotificationAsync(string conversationId, MessageResponse message);
}

public class MessagingService : IMessagingService
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly INotificationService _notificationService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<MessagingService> _logger;

    public MessagingService(
        ApplicationDbContext context,
        IBlobStorageService blobStorageService,
        INotificationService notificationService,
        IAnalyticsService analyticsService,
        ILogger<MessagingService> logger)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _notificationService = notificationService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<ServiceResult<List<ConversationResponse>>> GetConversationsAsync(string userId)
    {
        try
        {
            var conversations = await _context.Conversations
                .Where(c => c.Match.User1Id == userId || c.Match.User2Id == userId)
                .Where(c => c.IsActive)
                .Include(c => c.Match)
                .ThenInclude(m => m.User1)
                .ThenInclude(u => u.UserProfile)
                .ThenInclude(up => up.Photos)
                .Include(c => c.Match)
                .ThenInclude(m => m.User2)
                .ThenInclude(u => u.UserProfile)
                .ThenInclude(up => up.Photos)
                .Include(c => c.Messages.OrderByDescending(msg => msg.CreatedAt).Take(1))
                .OrderByDescending(c => c.Messages.Any() ? c.Messages.Max(m => m.CreatedAt) : c.CreatedAt)
                .ToListAsync();

            var conversationResponses = conversations.Select(conversation =>
            {
                var otherUser = conversation.Match.User1Id == userId ? conversation.Match.User2 : conversation.Match.User1;
                var lastMessage = conversation.Messages.FirstOrDefault();
                var unreadCount = conversation.Messages
                    .Count(m => m.SenderId != userId && !m.MessageReads.Any(mr => mr.UserId == userId));

                return new ConversationResponse
                {
                    Id = conversation.Id,
                    MatchId = conversation.MatchId,
                    OtherUser = new UserProfileResponse
                    {
                        Id = otherUser.Id,
                        Name = otherUser.UserProfile?.FullName ?? "User",
                        Age = otherUser.Age,
                        Photos = otherUser.UserProfile?.Photos?.OrderBy(p => p.Order)
                            .Select(p => new PhotoResponse
                            {
                                Id = p.Id,
                                Url = p.Url,
                                Order = p.Order,
                                IsMain = p.IsMain
                            }).Take(1).ToList() ?? new List<PhotoResponse>(),
                        IsVerified = otherUser.IsVerified,
                        LastActive = otherUser.LastActive
                    },
                    LastMessage = lastMessage != null ? new MessageResponse
                    {
                        Id = lastMessage.Id,
                        Content = lastMessage.Content,
                        Type = lastMessage.Type,
                        CreatedAt = lastMessage.CreatedAt,
                        SenderId = lastMessage.SenderId,
                        MediaUrl = lastMessage.MediaUrl,
                        IsRead = lastMessage.MessageReads.Any(mr => mr.UserId == userId)
                    } : null,
                    UnreadCount = unreadCount,
                    CreatedAt = conversation.CreatedAt,
                    IsActive = conversation.IsActive
                };
            }).ToList();

            return ServiceResult<List<ConversationResponse>>.Success(conversationResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
            return ServiceResult<List<ConversationResponse>>.Failure("An error occurred while getting conversations");
        }
    }

    public async Task<ServiceResult<PaginatedResponse<MessageResponse>>> GetMessagesAsync(string userId, string conversationId, int page = 1, int pageSize = 50)
    {
        try
        {
            // Verify user has access to conversation
            var hasAccess = await UserHasAccessToConversationAsync(userId, conversationId);
            if (!hasAccess)
            {
                return ServiceResult<PaginatedResponse<MessageResponse>>.Failure("Access denied to conversation");
            }

            var totalMessages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .CountAsync();

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .Include(m => m.MessageReads)
                .Include(m => m.MessageReactions)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var messageResponses = messages.Select(message => new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Content = message.Content,
                Type = message.Type,
                MediaUrl = message.MediaUrl,
                ThumbnailUrl = message.ThumbnailUrl,
                Duration = message.Duration,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt,
                IsRead = message.MessageReads.Any(mr => mr.UserId == userId),
                IsEdited = message.UpdatedAt.HasValue,
                Reactions = message.MessageReactions.GroupBy(mr => mr.Reaction)
                    .Select(g => new MessageReactionResponse
                    {
                        Reaction = g.Key,
                        Count = g.Count(),
                        UserIds = g.Select(x => x.UserId).ToList()
                    }).ToList()
            }).OrderBy(m => m.CreatedAt).ToList();

            var paginatedResponse = new PaginatedResponse<MessageResponse>
            {
                Items = messageResponses,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalMessages,
                TotalPages = (int)Math.Ceiling((double)totalMessages / pageSize)
            };

            return ServiceResult<PaginatedResponse<MessageResponse>>.Success(paginatedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            return ServiceResult<PaginatedResponse<MessageResponse>>.Failure("An error occurred while getting messages");
        }
    }

    public async Task<ServiceResult<MessageResponse>> SendMessageAsync(string userId, SendMessageRequest request)
    {
        try
        {
            // Verify user has access to conversation
            var hasAccess = await UserHasAccessToConversationAsync(userId, request.ConversationId);
            if (!hasAccess)
            {
                return ServiceResult<MessageResponse>.Failure("Access denied to conversation");
            }

            // Create message
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId,
                SenderId = userId,
                Content = request.Content,
                Type = request.Type,
                MediaUrl = request.MediaUrl,
                ThumbnailUrl = request.ThumbnailUrl,
                Duration = request.Duration,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Create response
            var messageResponse = new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Content = message.Content,
                Type = message.Type,
                MediaUrl = message.MediaUrl,
                ThumbnailUrl = message.ThumbnailUrl,
                Duration = message.Duration,
                CreatedAt = message.CreatedAt,
                IsRead = false,
                Reactions = new List<MessageReactionResponse>()
            };

            return ServiceResult<MessageResponse>.Success(messageResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for user {UserId}", userId);
            return ServiceResult<MessageResponse>.Failure("An error occurred while sending the message");
        }
    }

    public async Task<ServiceResult<MessageResponse>> UpdateMessageAsync(string userId, string messageId, string newContent)
    {
        try
        {
            var message = await _context.Messages
                .Include(m => m.MessageReads)
                .Include(m => m.MessageReactions)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return ServiceResult<MessageResponse>.Failure("Message not found");
            }

            if (message.SenderId != userId)
            {
                return ServiceResult<MessageResponse>.Failure("You can only edit your own messages");
            }

            // Check if message is too old to edit (e.g., 15 minutes)
            if (DateTime.UtcNow - message.CreatedAt > TimeSpan.FromMinutes(15))
            {
                return ServiceResult<MessageResponse>.Failure("Message is too old to edit");
            }

            message.Content = newContent;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var messageResponse = new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Content = message.Content,
                Type = message.Type,
                MediaUrl = message.MediaUrl,
                ThumbnailUrl = message.ThumbnailUrl,
                Duration = message.Duration,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt,
                IsRead = message.MessageReads.Any(),
                IsEdited = true,
                Reactions = message.MessageReactions.GroupBy(mr => mr.Reaction)
                    .Select(g => new MessageReactionResponse
                    {
                        Reaction = g.Key,
                        Count = g.Count(),
                        UserIds = g.Select(x => x.UserId).ToList()
                    }).ToList()
            };

            return ServiceResult<MessageResponse>.Success(messageResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating message {MessageId} for user {UserId}", messageId, userId);
            return ServiceResult<MessageResponse>.Failure("An error occurred while updating the message");
        }
    }

    public async Task<ServiceResult<bool>> DeleteMessageAsync(string userId, string messageId)
    {
        try
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return ServiceResult<bool>.Failure("Message not found");
            }

            if (message.SenderId != userId)
            {
                return ServiceResult<bool>.Failure("You can only delete your own messages");
            }

            message.IsDeleted = true;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId} for user {UserId}", messageId, userId);
            return ServiceResult<bool>.Failure("An error occurred while deleting the message");
        }
    }

    public async Task<ServiceResult<bool>> MarkMessageAsReadAsync(string userId, string messageId)
    {
        try
        {
            var message = await _context.Messages
                .Include(m => m.MessageReads)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return ServiceResult<bool>.Failure("Message not found");
            }

            // Don't mark own messages as read
            if (message.SenderId == userId)
            {
                return ServiceResult<bool>.Success(true);
            }

            // Check if already marked as read
            var existingRead = message.MessageReads.FirstOrDefault(mr => mr.UserId == userId);
            if (existingRead != null)
            {
                return ServiceResult<bool>.Success(true);
            }

            // Create read record
            var messageRead = new MessageRead
            {
                Id = Guid.NewGuid().ToString(),
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            };

            _context.MessageReads.Add(messageRead);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read {MessageId} for user {UserId}", messageId, userId);
            return ServiceResult<bool>.Failure("An error occurred while marking the message as read");
        }
    }

    public async Task<ServiceResult<bool>> MarkConversationAsReadAsync(string userId, string conversationId)
    {
        try
        {
            var hasAccess = await UserHasAccessToConversationAsync(userId, conversationId);
            if (!hasAccess)
            {
                return ServiceResult<bool>.Failure("Access denied to conversation");
            }

            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && 
                           m.SenderId != userId && 
                           !m.MessageReads.Any(mr => mr.UserId == userId))
                .ToListAsync();

            var messageReads = unreadMessages.Select(message => new MessageRead
            {
                Id = Guid.NewGuid().ToString(),
                MessageId = message.Id,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            }).ToList();

            _context.MessageReads.AddRange(messageReads);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking conversation as read {ConversationId} for user {UserId}", conversationId, userId);
            return ServiceResult<bool>.Failure("An error occurred while marking the conversation as read");
        }
    }

    public async Task<ServiceResult<bool>> AddMessageReactionAsync(string userId, string messageId, string reaction)
    {
        try
        {
            var message = await _context.Messages
                .Include(m => m.MessageReactions)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return ServiceResult<bool>.Failure("Message not found");
            }

            // Check if user already reacted with this reaction
            var existingReaction = message.MessageReactions
                .FirstOrDefault(mr => mr.UserId == userId && mr.Reaction == reaction);

            if (existingReaction != null)
            {
                return ServiceResult<bool>.Success(true);
            }

            // Remove any existing reaction from this user on this message
            var userReactions = message.MessageReactions.Where(mr => mr.UserId == userId).ToList();
            _context.MessageReactions.RemoveRange(userReactions);

            // Add new reaction
            var messageReaction = new MessageReaction
            {
                Id = Guid.NewGuid().ToString(),
                MessageId = messageId,
                UserId = userId,
                Reaction = reaction,
                CreatedAt = DateTime.UtcNow
            };

            _context.MessageReactions.Add(messageReaction);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction to message {MessageId} for user {UserId}", messageId, userId);
            return ServiceResult<bool>.Failure("An error occurred while adding the reaction");
        }
    }

    public async Task<ServiceResult<bool>> RemoveMessageReactionAsync(string userId, string messageId, string reaction)
    {
        try
        {
            var messageReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && 
                                          mr.UserId == userId && 
                                          mr.Reaction == reaction);

            if (messageReaction == null)
            {
                return ServiceResult<bool>.Success(true);
            }

            _context.MessageReactions.Remove(messageReaction);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from message {MessageId} for user {UserId}", messageId, userId);
            return ServiceResult<bool>.Failure("An error occurred while removing the reaction");
        }
    }

    public async Task<ServiceResult<MediaUploadResponse>> UploadMessageMediaAsync(string userId, UploadMediaRequest request)
    {
        try
        {
            if (request.File == null || request.File.Length == 0)
            {
                return ServiceResult<MediaUploadResponse>.Failure("No file provided");
            }

            // Upload to blob storage
            var uploadResult = await _blobStorageService.UploadFileAsync(
                request.File, 
                "message-media", 
                userId);

            if (!uploadResult.Success)
            {
                return ServiceResult<MediaUploadResponse>.Failure(uploadResult.Message);
            }

            var response = new MediaUploadResponse
            {
                Url = uploadResult.Data?.Url,
                ThumbnailUrl = uploadResult.Data?.ThumbnailUrl,
                FileName = request.File.FileName,
                FileSize = request.File.Length,
                ContentType = request.File.ContentType
            };

            return ServiceResult<MediaUploadResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media for user {UserId}", userId);
            return ServiceResult<MediaUploadResponse>.Failure("An error occurred while uploading the media");
        }
    }

    // Implement remaining interface methods...
    public async Task<ServiceResult<List<MediaResponse>>> GetConversationMediaAsync(string userId, string conversationId, string type)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> TrackTypingActivityAsync(string userId, string conversationId, bool isTyping)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<int>> GetUnreadMessageCountAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<bool>> UpdateUserOnlineStatusAsync(string userId, bool isOnline)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> UserHasAccessToConversationAsync(string userId, string conversationId)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Match)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            return conversation != null && 
                   (conversation.Match.User1Id == userId || conversation.Match.User2Id == userId);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetConversationIdForMessageAsync(string messageId)
    {
        try
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            return message?.ConversationId;
        }
        catch
        {
            return null;
        }
    }

    public async Task SendMessageNotificationAsync(string conversationId, MessageResponse message)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Match)
                .ThenInclude(m => m.User1)
                .Include(c => c.Match)
                .ThenInclude(m => m.User2)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null) return;

            var targetUser = conversation.Match.User1Id == message.SenderId 
                ? conversation.Match.User2 
                : conversation.Match.User1;

            await _notificationService.SendMessageNotificationAsync(targetUser.Id, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message notification for conversation {ConversationId}", conversationId);
        }
    }
}