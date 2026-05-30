namespace MemoryWeave.Client.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a personality/profile in the system.
/// Each personality has its own isolated data (entities, events, messages, etc.)
/// </summary>
public class PersonalityModel
{
    /// <summary>Unique identifier for the personality</summary>
    public int Id { get; set; }

    /// <summary>Name of the personality (e.g., "Me", "Mom", "Dad")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of the personality</summary>
    public string? Description { get; set; }

    /// <summary>Telegram phone number associated with this personality</summary>
    public string? TelegramPhone { get; set; }

    /// <summary>Path to Obsidian vault folder for this personality</summary>
    public string? ObsidianFolder { get; set; }

    /// <summary>Description of the personality's communication style</summary>
    public string? StyleDescription { get; set; }

    /// <summary>Whether this personality is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the personality was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the personality was last updated</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detailed personality information with statistics
/// </summary>
public class PersonalityDetailedModel : PersonalityModel
{
    /// <summary>Number of entities (people, places, etc.) for this personality</summary>
    public int EntityCount { get; set; }

    /// <summary>Number of relations between entities</summary>
    public int RelationCount { get; set; }

    /// <summary>Number of events recorded</summary>
    public int EventCount { get; set; }

    /// <summary>Number of messages synchronized</summary>
    public int MessageCount { get; set; }

    /// <summary>Timestamp of the last activity (message, event, etc.)</summary>
    public DateTime? LastActivity { get; set; }
}

/// <summary>
/// Request model for creating a new personality
/// </summary>
public class CreatePersonalityRequest
{
    /// <summary>Name of the personality</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description</summary>
    public string? Description { get; set; }

    /// <summary>Telegram phone number (optional)</summary>
    public string? TelegramPhone { get; set; }

    /// <summary>Obsidian folder path (optional)</summary>
    public string? ObsidianFolder { get; set; }

    /// <summary>Whether the personality is active</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating a personality
/// </summary>
public class UpdatePersonalityRequest
{
    /// <summary>New name (optional)</summary>
    public string? Name { get; set; }

    /// <summary>New description (optional)</summary>
    public string? Description { get; set; }

    /// <summary>New Telegram phone (optional)</summary>
    public string? TelegramPhone { get; set; }

    /// <summary>New Obsidian folder (optional)</summary>
    public string? ObsidianFolder { get; set; }

    /// <summary>Update active status (optional)</summary>
    public bool? IsActive { get; set; }
}
