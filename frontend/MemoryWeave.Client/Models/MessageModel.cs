namespace MemoryWeave.Client.Models;

using System;

/// <summary>
/// Represents a message from Telegram or other source
/// </summary>
public class MessageModel
{
    /// <summary>Unique identifier for the message</summary>
    public int Id { get; set; }

    /// <summary>ID of the personality this message belongs to</summary>
    public int PersonalityId { get; set; }

    /// <summary>Source of the message (e.g., "telegram", "obsidian")</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Raw text of the message</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Name/ID of the sender</summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>Telegram chat ID (if applicable)</summary>
    public long? TelegramChatId { get; set; }

    /// <summary>Telegram message ID (if applicable)</summary>
    public long? TelegramMessageId { get; set; }

    /// <summary>Timestamp when the message was sent</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Whether this message has been processed by NLP pipeline</summary>
    public bool Processed { get; set; } = false;

    /// <summary>When the message was synced to the system</summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
