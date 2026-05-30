"""Pydantic schemas for API request/response validation."""

from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field, field_validator


# ============================================================================
# PERSONALITY SCHEMAS
# ============================================================================

class PersonalityBase(BaseModel):
    """Base schema for personality data."""
    name: str = Field(..., min_length=1, max_length=100, description="Personality name")
    description: Optional[str] = Field(None, max_length=500, description="Description")
    telegram_phone: Optional[str] = Field(None, description="Telegram phone number")
    obsidian_folder: Optional[str] = Field(None, description="Obsidian vault folder path")
    is_active: bool = Field(True, description="Is personality active")
    
    @field_validator('name')
    @classmethod
    def name_not_empty(cls, v):
        if not v or not v.strip():
            raise ValueError('Name cannot be empty')
        return v.strip()


class PersonalityCreate(PersonalityBase):
    """Schema for creating a new personality."""
    pass


class PersonalityUpdate(BaseModel):
    """Schema for updating a personality."""
    name: Optional[str] = Field(None, min_length=1, max_length=100)
    description: Optional[str] = Field(None, max_length=500)
    telegram_phone: Optional[str] = None
    obsidian_folder: Optional[str] = None
    is_active: Optional[bool] = None


class PersonalityResponse(PersonalityBase):
    """Schema for personality response."""
    id: int = Field(..., description="Personality ID")
    created_at: datetime = Field(..., description="Creation timestamp")
    updated_at: datetime = Field(..., description="Last update timestamp")
    
    model_config = {"from_attributes": True}


class PersonalityDetailedResponse(PersonalityResponse):
    """Detailed personality response with statistics."""
    entity_count: int = Field(0, description="Number of entities in this personality")
    relation_count: int = Field(0, description="Number of relations in this personality")
    event_count: int = Field(0, description="Number of events in this personality")
    message_count: int = Field(0, description="Number of messages in this personality")
    last_activity: Optional[datetime] = Field(None, description="Last activity timestamp")


# ============================================================================
# ENTITY SCHEMAS
# ============================================================================

class EntityBase(BaseModel):
    """Base schema for entity data."""
    personality_id: int = Field(..., description="Associated personality ID")
    type: str = Field(..., description="Entity type: person, place, org, project, date")
    name: str = Field(..., min_length=1, max_length=200)
    description: Optional[str] = None
    significance_score: int = Field(3, ge=1, le=5)


class EntityCreate(EntityBase):
    """Schema for creating an entity."""
    pass


class EntityResponse(EntityBase):
    """Schema for entity response."""
    id: int
    normalized_name: str
    mention_count: int
    created_at: datetime
    updated_at: datetime
    
    model_config = {"from_attributes": True}


# ============================================================================
# MESSAGE SCHEMAS
# ============================================================================

class MessageBase(BaseModel):
    """Base schema for message data."""
    personality_id: int
    source: str = Field(..., description="Source: telegram, obsidian, etc")
    source_id: str
    text: str
    significance_score: int = Field(3, ge=1, le=5)


class MessageCreate(MessageBase):
    """Schema for creating a message."""
    pass


class MessageResponse(MessageBase):
    """Schema for message response."""
    id: int
    timestamp: datetime
    processed: bool
    created_at: datetime
    
    model_config = {"from_attributes": True}


# ============================================================================
# ERROR SCHEMAS
# ============================================================================

class ErrorResponse(BaseModel):
    """Schema for error responses."""
    detail: str
    error_code: Optional[str] = None
    status_code: int


# ============================================================================
# BULK OPERATION SCHEMAS
# ============================================================================

class BulkDeleteRequest(BaseModel):
    """Schema for bulk delete request."""
    ids: list[int] = Field(..., min_length=1, max_length=100)


class BulkDeleteResponse(BaseModel):
    """Schema for bulk delete response."""
    deleted_count: int
    failed_ids: list[int] = []


# ============================================================================
# STATISTICS SCHEMAS
# ============================================================================

class PersonalityStats(BaseModel):
    """Statistics for a personality."""
    personality_id: int
    entity_count: int
    relation_count: int
    event_count: int
    message_count: int
    unprocessed_messages: int
    last_activity: Optional[datetime]


class DatabaseStats(BaseModel):
    """Database-wide statistics."""
    total_personalities: int
    total_entities: int
    total_messages: int
    total_events: int
    db_size_mb: float
    personalities_stats: list[PersonalityStats]
