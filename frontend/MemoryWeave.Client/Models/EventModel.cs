namespace MemoryWeave.Client.Models;

using System;

/// <summary>
/// Represents a significant event in the personality's memory
/// </summary>
public class EventModel
{
    /// <summary>Unique identifier for the event</summary>
    public int Id { get; set; }

    /// <summary>ID of the personality this event belongs to</summary>
    public int PersonalityId { get; set; }

    /// <summary>Brief title of the event</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detailed description of the event</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>When the event occurred</summary>
    public DateTime EventDate { get; set; } = DateTime.UtcNow;

    /// <summary>Category of the event (e.g., "meeting", "birthday", "achievement")</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>IDs of entities involved in this event</summary>
    public string? EntityIds { get; set; }

    /// <summary>Significance score (0-1)</summary>
    public float Significance { get; set; } = 0.5f;

    /// <summary>Source of the event (which message/note led to this event)</summary>
    public int? SourceMessageId { get; set; }

    /// <summary>When the event was created in the system</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
