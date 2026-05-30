"""API endpoints for personality management."""

import logging
from typing import List

from fastapi import APIRouter, Depends, HTTPException, Query, status
from sqlalchemy import func, text
from sqlalchemy.orm import Session

from backend.app.db.database import get_db
from backend.app.models import (
    Personality, Entity, Relation, Event, Message
)
from backend.app.schemas import (
    PersonalityCreate, PersonalityUpdate, PersonalityResponse,
    PersonalityDetailedResponse, ErrorResponse, BulkDeleteRequest,
    BulkDeleteResponse
)

logger = logging.getLogger(__name__)

router = APIRouter(
    prefix="/api/personalities",
    tags=["personalities"],
    responses={
        404: {"model": ErrorResponse, "description": "Not found"},
        400: {"model": ErrorResponse, "description": "Bad request"},
        500: {"model": ErrorResponse, "description": "Internal server error"},
    },
)


# ============================================================================
# CREATE
# ============================================================================

@router.post(
    "",
    response_model=PersonalityResponse,
    status_code=status.HTTP_201_CREATED,
    summary="Create a new personality",
    description="Create a new personality for the user. Each personality can have its own set of entities, events, and memories."
)
async def create_personality(
    personality: PersonalityCreate,
    db: Session = Depends(get_db)
) -> PersonalityResponse:
    """
    Create a new personality.
    
    - **name**: Personality name (required)
    - **description**: Optional description
    - **telegram_phone**: Optional Telegram phone number
    - **obsidian_folder**: Optional Obsidian vault folder path
    - **is_active**: Whether personality is active (default: true)
    """
    try:
        # Check if personality with this name already exists
        existing = db.query(Personality).filter(
            func.lower(Personality.name) == personality.name.lower()
        ).first()
        
        if existing:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Personality '{personality.name}' already exists"
            )
        
        # Create new personality
        new_personality = Personality(
            name=personality.name,
            description=personality.description,
            telegram_phone=personality.telegram_phone,
            obsidian_folder=personality.obsidian_folder,
            is_active=personality.is_active
        )
        
        db.add(new_personality)
        db.commit()
        db.refresh(new_personality)
        
        logger.info(f"Created personality '{new_personality.name}' (ID: {new_personality.id})")
        
        return PersonalityResponse.model_validate(new_personality)
    
    except HTTPException:
        raise
    except Exception as e:
        db.rollback()
        logger.error(f"Error creating personality: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to create personality"
        )


# ============================================================================
# READ
# ============================================================================

@router.get(
    "",
    response_model=List[PersonalityResponse],
    summary="List all personalities",
    description="Get a list of all personalities, with optional filtering and pagination."
)
async def list_personalities(
    skip: int = Query(0, ge=0, description="Number of personalities to skip"),
    limit: int = Query(10, ge=1, le=100, description="Number of personalities to return"),
    is_active: bool = Query(None, description="Filter by active status"),
    db: Session = Depends(get_db)
) -> List[PersonalityResponse]:
    """
    List all personalities with pagination and optional filtering.
    
    - **skip**: Number of results to skip (pagination)
    - **limit**: Max number of results to return (1-100)
    - **is_active**: Filter by active status (optional)
    """
    try:
        query = db.query(Personality)
        
        if is_active is not None:
            query = query.filter(Personality.is_active == is_active)
        
        personalities = query.order_by(Personality.updated_at.desc()).offset(skip).limit(limit).all()
        
        return [PersonalityResponse.model_validate(p) for p in personalities]
    
    except Exception as e:
        logger.error(f"Error listing personalities: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to list personalities"
        )


@router.get(
    "/{personality_id}",
    response_model=PersonalityResponse,
    summary="Get personality by ID",
    description="Get detailed information about a specific personality."
)
async def get_personality(
    personality_id: int,
    db: Session = Depends(get_db)
) -> PersonalityResponse:
    """
    Get a personality by its ID.
    
    - **personality_id**: The personality ID
    """
    personality = db.query(Personality).filter(
        Personality.id == personality_id
    ).first()
    
    if not personality:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Personality with ID {personality_id} not found"
        )
    
    return PersonalityResponse.model_validate(personality)


@router.get(
    "/{personality_id}/detailed",
    response_model=PersonalityDetailedResponse,
    summary="Get personality with statistics",
    description="Get detailed information about a personality including statistics."
)
async def get_personality_detailed(
    personality_id: int,
    db: Session = Depends(get_db)
) -> PersonalityDetailedResponse:
    """
    Get detailed information about a personality including statistics.
    
    - **personality_id**: The personality ID
    """
    personality = db.query(Personality).filter(
        Personality.id == personality_id
    ).first()
    
    if not personality:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Personality with ID {personality_id} not found"
        )
    
    # Count related entities
    entity_count = db.query(func.count(Entity.id)).filter(
        Entity.personality_id == personality_id
    ).scalar() or 0
    
    relation_count = db.query(func.count(Relation.id)).filter(
        Relation.personality_id == personality_id
    ).scalar() or 0
    
    event_count = db.query(func.count(Event.id)).filter(
        Event.personality_id == personality_id
    ).scalar() or 0
    
    message_count = db.query(func.count(Message.id)).filter(
        Message.personality_id == personality_id
    ).scalar() or 0
    
    # Get last activity
    last_activity = db.query(func.max(Message.timestamp)).filter(
        Message.personality_id == personality_id
    ).scalar()
    
    response_data = PersonalityResponse.model_validate(personality).model_dump()
    response_data.update({
        "entity_count": entity_count,
        "relation_count": relation_count,
        "event_count": event_count,
        "message_count": message_count,
        "last_activity": last_activity
    })
    
    return PersonalityDetailedResponse(**response_data)


# ============================================================================
# UPDATE
# ============================================================================

@router.patch(
    "/{personality_id}",
    response_model=PersonalityResponse,
    summary="Update personality",
    description="Update one or more fields of a personality."
)
async def update_personality(
    personality_id: int,
    personality_update: PersonalityUpdate,
    db: Session = Depends(get_db)
) -> PersonalityResponse:
    """
    Update a personality's information.
    
    - **personality_id**: The personality ID
    - **personality_update**: Fields to update (all optional)
    """
    try:
        personality = db.query(Personality).filter(
            Personality.id == personality_id
        ).first()
        
        if not personality:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Personality with ID {personality_id} not found"
            )
        
        # Check if new name already exists
        if personality_update.name and personality_update.name != personality.name:
            existing = db.query(Personality).filter(
                func.lower(Personality.name) == personality_update.name.lower()
            ).first()
            if existing:
                raise HTTPException(
                    status_code=status.HTTP_400_BAD_REQUEST,
                    detail=f"Personality '{personality_update.name}' already exists"
                )
        
        # Update fields
        update_data = personality_update.model_dump(exclude_unset=True)
        for field, value in update_data.items():
            setattr(personality, field, value)
        
        db.commit()
        db.refresh(personality)
        
        logger.info(f"Updated personality '{personality.name}' (ID: {personality.id})")
        
        return PersonalityResponse.model_validate(personality)
    
    except HTTPException:
        raise
    except Exception as e:
        db.rollback()
        logger.error(f"Error updating personality: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to update personality"
        )


# ============================================================================
# DELETE
# ============================================================================

@router.delete(
    "/{personality_id}",
    status_code=status.HTTP_204_NO_CONTENT,
    summary="Delete personality",
    description="Delete a personality and all its associated data (irreversible)."
)
async def delete_personality(
    personality_id: int,
    db: Session = Depends(get_db)
) -> None:
    """
    Delete a personality and all its associated data.
    
    ⚠️ This action is irreversible and will delete all entities, events, and messages
    associated with this personality.
    
    - **personality_id**: The personality ID to delete
    """
    try:
        personality = db.query(Personality).filter(
            Personality.id == personality_id
        ).first()
        
        if not personality:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Personality with ID {personality_id} not found"
            )
        
        personality_name = personality.name
        
        # Delete personality and all associated data (cascade)
        db.delete(personality)
        db.commit()
        
        logger.warning(f"Deleted personality '{personality_name}' (ID: {personality_id}) and all associated data")
    
    except HTTPException:
        raise
    except Exception as e:
        db.rollback()
        logger.error(f"Error deleting personality: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to delete personality"
        )


# ============================================================================
# BULK OPERATIONS
# ============================================================================

@router.post(
    "/bulk/delete",
    response_model=BulkDeleteResponse,
    summary="Bulk delete personalities",
    description="Delete multiple personalities at once."
)
async def bulk_delete_personalities(
    request: BulkDeleteRequest,
    db: Session = Depends(get_db)
) -> BulkDeleteResponse:
    """
    Delete multiple personalities in a single request.
    
    - **ids**: List of personality IDs to delete (1-100 items)
    """
    try:
        # Fetch personalities to delete
        personalities = db.query(Personality).filter(
            Personality.id.in_(request.ids)
        ).all()
        
        deleted_count = 0
        failed_ids = []
        
        for personality in personalities:
            try:
                db.delete(personality)
                deleted_count += 1
            except Exception as e:
                logger.error(f"Error deleting personality {personality.id}: {e}")
                failed_ids.append(personality.id)
        
        # Check for IDs that weren't found
        found_ids = {p.id for p in personalities}
        for requested_id in request.ids:
            if requested_id not in found_ids:
                failed_ids.append(requested_id)
        
        db.commit()
        
        logger.info(f"Bulk deleted {deleted_count} personalities (failed: {len(failed_ids)})")
        
        return BulkDeleteResponse(
            deleted_count=deleted_count,
            failed_ids=failed_ids
        )
    
    except Exception as e:
        db.rollback()
        logger.error(f"Error in bulk delete: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to bulk delete personalities"
        )


# ============================================================================
# STATISTICS
# ============================================================================

@router.get(
    "/{personality_id}/stats",
    summary="Get personality statistics",
    description="Get detailed statistics for a personality."
)
async def get_personality_stats(
    personality_id: int,
    db: Session = Depends(get_db)
):
    """
    Get comprehensive statistics for a personality.
    
    - **personality_id**: The personality ID
    """
    personality = db.query(Personality).filter(
        Personality.id == personality_id
    ).first()
    
    if not personality:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Personality with ID {personality_id} not found"
        )
    
    try:
        # Get various statistics
        stats = {
            "personality_id": personality_id,
            "name": personality.name,
            "entity_count": db.query(func.count(Entity.id)).filter(
                Entity.personality_id == personality_id
            ).scalar() or 0,
            "entity_by_type": {
                row[0]: row[1] for row in db.query(
                    Entity.type,
                    func.count(Entity.id)
                ).filter(Entity.personality_id == personality_id).group_by(
                    Entity.type
                ).all()
            },
            "relation_count": db.query(func.count(Relation.id)).filter(
                Relation.personality_id == personality_id
            ).scalar() or 0,
            "event_count": db.query(func.count(Event.id)).filter(
                Event.personality_id == personality_id
            ).scalar() or 0,
            "message_count": db.query(func.count(Message.id)).filter(
                Message.personality_id == personality_id
            ).scalar() or 0,
            "unprocessed_messages": db.query(func.count(Message.id)).filter(
                Message.personality_id == personality_id,
                Message.processed == False
            ).scalar() or 0,
            "last_activity": db.query(func.max(Message.timestamp)).filter(
                Message.personality_id == personality_id
            ).scalar()
        }
        
        return stats
    
    except Exception as e:
        logger.error(f"Error getting personality stats: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to get personality statistics"
        )
