using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneTime.API.Models.DTOs;
using OneTime.API.Services;
using System.Security.Claims;

namespace OneTime.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagingController : ControllerBase
{
    private readonly IMessagingService _messagingService;
    private readonly ILogger<MessagingController> _logger;

    public MessagingController(
        IMessagingService messagingService,
        ILogger<MessagingController> logger)
    {
        _messagingService = messagingService;
        _logger = logger;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.GetConversationsAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<ConversationResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Conversations retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<ConversationResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(string conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.GetMessagesAsync(userId, conversationId, page, pageSize);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<PaginatedResponse<MessageResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Messages retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<PaginatedResponse<MessageResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(string conversationId, [FromBody] SendMessageRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        request.ConversationId = conversationId;
        var result = await _messagingService.SendMessageAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<MessageResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Message sent successfully"
            });
        }

        return BadRequest(new ApiResponse<MessageResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPut("messages/{messageId}")]
    public async Task<IActionResult> UpdateMessage(string messageId, [FromBody] UpdateMessageRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.UpdateMessageAsync(userId, messageId, request.Content);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<MessageResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Message updated successfully"
            });
        }

        return BadRequest(new ApiResponse<MessageResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(string messageId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.DeleteMessageAsync(userId, messageId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Message deleted successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("messages/{messageId}/read")]
    public async Task<IActionResult> MarkMessageAsRead(string messageId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.MarkMessageAsReadAsync(userId, messageId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Message marked as read"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkConversationAsRead(string conversationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.MarkConversationAsReadAsync(userId, conversationId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Conversation marked as read"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("messages/{messageId}/reactions")]
    public async Task<IActionResult> AddReaction(string messageId, [FromBody] AddReactionRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.AddMessageReactionAsync(userId, messageId, request.Reaction);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Reaction added successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpDelete("messages/{messageId}/reactions/{reaction}")]
    public async Task<IActionResult> RemoveReaction(string messageId, string reaction)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.RemoveMessageReactionAsync(userId, messageId, reaction);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Reaction removed successfully"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMedia([FromForm] UploadMediaRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.UploadMessageMediaAsync(userId, request);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<MediaUploadResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "Media uploaded successfully"
            });
        }

        return BadRequest(new ApiResponse<MediaUploadResponse>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("conversations/{conversationId}/media")]
    public async Task<IActionResult> GetConversationMedia(string conversationId, [FromQuery] string type = "all")
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.GetConversationMediaAsync(userId, conversationId, type);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<List<MediaResponse>>
            {
                Success = true,
                Data = result.Data,
                Message = "Media retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<List<MediaResponse>>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("typing")]
    public async Task<IActionResult> SetTypingStatus([FromBody] TypingStatusRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.TrackTypingActivityAsync(userId, request.ConversationId, request.IsTyping);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = result.Data,
                Message = "Typing status updated"
            });
        }

        return BadRequest(new ApiResponse<bool>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _messagingService.GetUnreadMessageCountAsync(userId);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = result.Data,
                Message = "Unread count retrieved successfully"
            });
        }

        return BadRequest(new ApiResponse<int>
        {
            Success = false,
            Message = result.Message
        });
    }

    [HttpPost("conversations/{conversationId}/gif")]
    public async Task<IActionResult> SendGif(string conversationId, [FromBody] SendGifRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var messageRequest = new SendMessageRequest
        {
            ConversationId = conversationId,
            Type = "gif",
            MediaUrl = request.GifUrl,
            Content = request.SearchTerm
        };

        var result = await _messagingService.SendMessageAsync(userId, messageRequest);
        
        if (result.Success)
        {
            return Ok(new ApiResponse<MessageResponse>
            {
                Success = true,
                Data = result.Data,
                Message = "GIF sent successfully"
            });
        }

        return BadRequest(new ApiResponse<MessageResponse>
        {
            Success = false,
            Message = result.Message
        });
    }
}