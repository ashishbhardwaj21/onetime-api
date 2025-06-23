using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OneTime.API.Models.DTOs;
using OneTime.API.Services;
using System.Security.Claims;

namespace OneTime.API.Hubs;

[Authorize]
public class MessageHub : Hub
{
    private readonly IMessagingService _messagingService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<MessageHub> _logger;

    public MessageHub(
        IMessagingService messagingService,
        IAnalyticsService analyticsService,
        ILogger<MessageHub> logger)
    {
        _messagingService = messagingService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // Join user to their personal group for notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Update user online status
            await _messagingService.UpdateUserOnlineStatusAsync(userId, true);
            
            // Notify contacts that user is online
            await Clients.Others.SendAsync("UserOnline", userId);
            
            _logger.LogInformation("User {UserId} connected to MessageHub", userId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // Remove from personal group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Update user offline status
            await _messagingService.UpdateUserOnlineStatusAsync(userId, false);
            
            // Notify contacts that user is offline
            await Clients.Others.SendAsync("UserOffline", userId);
            
            _logger.LogInformation("User {UserId} disconnected from MessageHub", userId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(string conversationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        try
        {
            // Verify user has access to this conversation
            var hasAccess = await _messagingService.UserHasAccessToConversationAsync(userId, conversationId);
            if (!hasAccess)
            {
                await Clients.Caller.SendAsync("Error", "Access denied to conversation");
                return;
            }

            // Join the conversation group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            
            // Mark user as typing in this conversation initially as false
            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("UserJoinedConversation", userId, conversationId);
            
            _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining conversation {ConversationId} for user {UserId}", conversationId, userId);
            await Clients.Caller.SendAsync("Error", "Failed to join conversation");
        }
    }

    public async Task LeaveConversation(string conversationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            // Leave the conversation group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            
            // Stop any typing indicators
            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("UserStoppedTyping", userId, conversationId);
            
            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("UserLeftConversation", userId, conversationId);
            
            _logger.LogInformation("User {UserId} left conversation {ConversationId}", userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving conversation {ConversationId} for user {UserId}", conversationId, userId);
        }
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        try
        {
            // Send message through messaging service
            var result = await _messagingService.SendMessageAsync(userId, request);
            
            if (result.Success && result.Data != null)
            {
                // Broadcast message to conversation participants
                await Clients.Group($"conversation_{request.ConversationId}")
                    .SendAsync("MessageReceived", result.Data);
                
                // Send push notification to offline users
                await _messagingService.SendMessageNotificationAsync(request.ConversationId, result.Data);
                
                // Track analytics
                await _analyticsService.TrackEventAsync(userId, "message_sent", new Dictionary<string, object>
                {
                    {"conversation_id", request.ConversationId},
                    {"message_type", request.Type},
                    {"message_length", request.Content?.Length ?? 0}
                });
                
                _logger.LogInformation("Message sent by user {UserId} in conversation {ConversationId}", 
                    userId, request.ConversationId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message ?? "Failed to send message");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    public async Task StartTyping(string conversationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            // Verify user has access to conversation
            var hasAccess = await _messagingService.UserHasAccessToConversationAsync(userId, conversationId);
            if (!hasAccess)
            {
                return;
            }

            // Notify other participants that user is typing
            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("UserStartedTyping", userId, conversationId);
            
            // Track typing activity
            await _messagingService.TrackTypingActivityAsync(userId, conversationId, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling start typing for user {UserId} in conversation {ConversationId}", 
                userId, conversationId);
        }
    }

    public async Task StopTyping(string conversationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            // Notify other participants that user stopped typing
            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("UserStoppedTyping", userId, conversationId);
            
            // Track typing activity
            await _messagingService.TrackTypingActivityAsync(userId, conversationId, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling stop typing for user {UserId} in conversation {ConversationId}", 
                userId, conversationId);
        }
    }

    public async Task MarkMessageAsRead(string messageId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            var result = await _messagingService.MarkMessageAsReadAsync(userId, messageId);
            
            if (result.Success)
            {
                // Get conversation ID for the message
                var conversationId = await _messagingService.GetConversationIdForMessageAsync(messageId);
                
                if (!string.IsNullOrEmpty(conversationId))
                {
                    // Notify other participants about read status
                    await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                        .SendAsync("MessageRead", messageId, userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read for user {UserId}, message {MessageId}", 
                userId, messageId);
        }
    }

    public async Task MarkConversationAsRead(string conversationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            var result = await _messagingService.MarkConversationAsReadAsync(userId, conversationId);
            
            if (result.Success)
            {
                // Notify other participants about conversation read status
                await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                    .SendAsync("ConversationRead", conversationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking conversation as read for user {UserId}, conversation {ConversationId}", 
                userId, conversationId);
        }
    }

    public async Task ReactToMessage(string messageId, string reaction)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            var result = await _messagingService.AddMessageReactionAsync(userId, messageId, reaction);
            
            if (result.Success)
            {
                // Get conversation ID for the message
                var conversationId = await _messagingService.GetConversationIdForMessageAsync(messageId);
                
                if (!string.IsNullOrEmpty(conversationId))
                {
                    // Notify all participants about the reaction
                    await Clients.Group($"conversation_{conversationId}")
                        .SendAsync("MessageReactionAdded", messageId, userId, reaction);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction to message for user {UserId}, message {MessageId}", 
                userId, messageId);
        }
    }

    public async Task RemoveReaction(string messageId, string reaction)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            var result = await _messagingService.RemoveMessageReactionAsync(userId, messageId, reaction);
            
            if (result.Success)
            {
                // Get conversation ID for the message
                var conversationId = await _messagingService.GetConversationIdForMessageAsync(messageId);
                
                if (!string.IsNullOrEmpty(conversationId))
                {
                    // Notify all participants about the reaction removal
                    await Clients.Group($"conversation_{conversationId}")
                        .SendAsync("MessageReactionRemoved", messageId, userId, reaction);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing reaction from message for user {UserId}, message {MessageId}", 
                userId, messageId);
        }
    }

    public async Task DeleteMessage(string messageId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            // Get conversation ID before deletion
            var conversationId = await _messagingService.GetConversationIdForMessageAsync(messageId);
            
            var result = await _messagingService.DeleteMessageAsync(userId, messageId);
            
            if (result.Success && !string.IsNullOrEmpty(conversationId))
            {
                // Notify all participants about the message deletion
                await Clients.Group($"conversation_{conversationId}")
                    .SendAsync("MessageDeleted", messageId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message for user {UserId}, message {MessageId}", 
                userId, messageId);
        }
    }

    public async Task UpdateMessage(string messageId, string newContent)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            var result = await _messagingService.UpdateMessageAsync(userId, messageId, newContent);
            
            if (result.Success && result.Data != null)
            {
                // Get conversation ID for the message
                var conversationId = await _messagingService.GetConversationIdForMessageAsync(messageId);
                
                if (!string.IsNullOrEmpty(conversationId))
                {
                    // Notify all participants about the message update
                    await Clients.Group($"conversation_{conversationId}")
                        .SendAsync("MessageUpdated", result.Data);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating message for user {UserId}, message {MessageId}", 
                userId, messageId);
        }
    }

    public async Task GetConnectionInfo()
    {
        var userId = Context.UserIdentifier;
        await Clients.Caller.SendAsync("ConnectionInfo", new
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            ConnectedAt = DateTime.UtcNow
        });
    }

    // Helper method to send notifications to specific users
    public async Task SendNotificationToUser(string targetUserId, string type, object data)
    {
        await Clients.Group($"user_{targetUserId}").SendAsync("Notification", type, data);
    }

    // Method to handle voice/video call signaling
    public async Task SendCallSignal(string targetUserId, string signal, object data)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            await Clients.Group($"user_{targetUserId}")
                .SendAsync("CallSignal", signal, userId, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending call signal from {UserId} to {TargetUserId}", 
                userId, targetUserId);
        }
    }
}