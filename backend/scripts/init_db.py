#!/usr/bin/env python
"""Initialize database with tables and default personalities."""

import logging
import sys
from pathlib import Path

# Add backend to path
sys.path.insert(0, str(Path(__file__).parent.parent.parent))

from backend.app.db.database import init_db, SessionLocal
from backend.app.models import Personality

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def create_default_personality():
    """Create default 'Me' personality if it doesn't exist."""
    db = SessionLocal()
    try:
        # Check if default personality exists
        default = db.query(Personality).filter(
            Personality.name == "Me"
        ).first()
        
        if default:
            logger.info("Default personality 'Me' already exists")
            return
        
        # Create default personality
        default_personality = Personality(
            name="Me",
            description="Main user personality",
            obsidian_folder="Me",
            is_active=True
        )
        
        db.add(default_personality)
        db.commit()
        logger.info(f"Created default personality 'Me' (ID: {default_personality.id})")
    
    except Exception as e:
        logger.error(f"Error creating default personality: {e}")
        db.rollback()
    finally:
        db.close()


def main():
    """Initialize the database."""
    try:
        logger.info("Initializing database...")
        init_db()
        logger.info("Database tables created successfully")
        
        # Create default personality
        create_default_personality()
        
        logger.info("Database initialization complete!")
    except Exception as e:
        logger.error(f"Failed to initialize database: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
