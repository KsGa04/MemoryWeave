namespace MemoryWeave.Client.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents an entity (person, place, project, etc.) in the memory graph
/// </summary>
public class EntityModel
{
    /// <summary>Unique identifier for the entity</summary>
    public int Id { get; set; }

    /// <summary>ID of the personality this entity belongs to</summary>
    public int PersonalityId { get; set; }

    /// <summary>Type of entity: "person", "place", "project", etc.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Name of the entity (normalized)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Aliases/alternative names for the entity</summary>
    public List<string>? Aliases { get; set; }

    /// <summary>Description or notes about the entity</summary>
    public string? Description { get; set; }

    /// <summary>First mention date</summary>
    public DateTime? FirstMentioned { get; set; }

    /// <summary>Last mention date</summary>
    public DateTime? LastMentioned { get; set; }

    /// <summary>Number of mentions</summary>
    public int MentionCount { get; set; } = 1;

    /// <summary>Significance score (0-1)</summary>
    public float Significance { get; set; } = 0.5f;

    /// <summary>When the entity was created in the system</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a relation/connection between two entities
/// </summary>
public class RelationModel
{
    /// <summary>Unique identifier for the relation</summary>
    public int Id { get; set; }

    /// <summary>ID of the personality this relation belongs to</summary>
    public int PersonalityId { get; set; }

    /// <summary>Source entity ID</summary>
    public int SourceEntityId { get; set; }

    /// <summary>Target entity ID</summary>
    public int TargetEntityId { get; set; }

    /// <summary>Type of relation (e.g., "knows", "works_with", "visited")</summary>
    public string RelationType { get; set; } = string.Empty;

    /// <summary>Description of the relation</summary>
    public string? Description { get; set; }

    /// <summary>When the relation was established</summary>
    public DateTime? EstablishedDate { get; set; }

    /// <summary>Strength/confidence of the relation (0-1)</summary>
    public float Strength { get; set; } = 0.5f;

    /// <summary>When the relation was discovered</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
