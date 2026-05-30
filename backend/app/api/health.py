"""Health check and system endpoints."""

import logging

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from backend.app.db.database import get_db, get_db_stats

logger = logging.getLogger(__name__)

router = APIRouter(
    prefix="/api",
    tags=["system"],
)


@router.get(
    "/health",
    summary="Health check",
    description="Check if the API is running and database is accessible."
)
async def health_check(db: Session = Depends(get_db)):
    """
    Health check endpoint that verifies database connectivity.
    """
    try:
        # Try to query the database
        db.execute("SELECT 1")
        
        return {
            "status": "healthy",
            "message": "API is running and database is accessible"
        }
    except Exception as e:
        logger.error(f"Health check failed: {e}")
        return {
            "status": "unhealthy",
            "message": f"Database error: {str(e)}"
        }


@router.get(
    "/stats",
    summary="Database statistics",
    description="Get comprehensive database statistics."
)
async def get_stats():
    """
    Get database statistics including table counts and file size.
    """
    try:
        stats = get_db_stats()
        return {
            "status": "success",
            "data": stats
        }
    except Exception as e:
        logger.error(f"Error getting stats: {e}")
        return {
            "status": "error",
            "message": str(e)
        }


@router.get("/")
async def root():
    """Root endpoint with API information."""
    return {
        "name": "MemoryWeave API",
        "version": "1.0.0",
        "description": "Personal information system with adaptive dialog agent",
        "endpoints": {
            "health": "/api/health",
            "stats": "/api/stats",
            "personalities": "/api/personalities",
            "docs": "/docs",
            "openapi": "/openapi.json"
        }
    }
