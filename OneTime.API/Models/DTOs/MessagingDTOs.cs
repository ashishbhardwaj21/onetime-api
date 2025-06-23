namespace OneTime.API.Models.DTOs;

// Request DTOs
public class SendMessageRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Type { get; set; } = "text"; // text, image, video, voice, gif
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Duration { get; set; } // For voice/video messages in seconds
}

public class UpdateMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class AddReactionRequest
{
    public string Reaction { get; set; } = string.Empty; // emoji or reaction type
}

public class UploadMediaRequest
{
    public IFormFile File { get; set; } = null!;
    public string Type { get; set; } = string.Empty; // image, video, voice
}

public class TypingStatusRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
}

public class SendGifRequest
{
    public string GifUrl { get; set; } = string.Empty;
    public string? SearchTerm { get; set; }
}

// Response DTOs
public class ConversationResponse
{
    public string Id { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
    public UserProfileResponse OtherUser { get; set; } = new();
    public MessageResponse? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class MessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsEdited { get; set; }
    public List<MessageReactionResponse> Reactions { get; set; } = new();
}

public class MessageReactionResponse
{
    public string Reaction { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> UserIds { get; set; } = new();
}

public class MediaUploadResponse
{
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
}

public class MediaResponse
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SenderId { get; set; } = string.Empty;
}