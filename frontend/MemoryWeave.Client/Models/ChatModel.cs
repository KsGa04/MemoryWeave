namespace MemoryWeave.Client.Models;

using System;

/// <summary>
/// Represents a chat message in the conversation with AI assistant
/// </summary>
public class ChatMessageModel
{
    /// <summary>Unique identifier for the chat message</summary>
    public int Id { get; set; }

    /// <summary>ID of the personality being chatted with</summary>
    public int PersonalityId { get; set; }

    /// <summary>The actual message text</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Whether this is a user message (true) or assistant response (false)</summary>
    public bool IsUser { get; set; }

    /// <summary>Timestamp of the message</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Source IDs of cited memories (if this is assistant response)</summary>
    public string? CitedSourceIds { get; set; }
}

/// <summary>
/// Request model for sending a chat message to the AI assistant
/// </summary>
public class ChatQueryRequest
{
    /// <summary>The question/message from the user</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>ID of the active personality</summary>
    public int PersonalityId { get; set; }

    /// <summary>Optional context or additional instructions</summary>
    public string? Context { get; set; }

    /// <summary>Number of relevant memories to retrieve</summary>
    public int TopK { get; set; } = 5;
}

/// <summary>
/// Response model from the AI assistant
/// </summary>
public class ChatQueryResponse
{
    /// <summary>The generated response text</summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>Sources (memory entries) that were used to generate the response</summary>
    public string[]? Sources { get; set; }

    /// <summary>Confidence score (0-1)</summary>
    public float Confidence { get; set; }

    /// <summary>Processing time in milliseconds</summary>
    public long ProcessingTimeMs { get; set; }
}
