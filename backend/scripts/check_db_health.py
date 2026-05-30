#!/usr/bin/env python
"""Check database health and print statistics."""

import logging
import sys
from pathlib import Path
from sqlalchemy import text, inspect

# Add backend to path
sys.path.insert(0, str(Path(__file__).parent.parent.parent))

from backend.app.db.database import SessionLocal, engine

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def check_database():
    """Check database connectivity and health."""
    try:
        db = SessionLocal()
        
        # Try a simple query
        result = db.execute(text("SELECT 1"))
        db.close()
        
        logger.info("✓ Database is healthy")
        return True
    
    except Exception as e:
        logger.error(f"✗ Database error: {e}")
        return False


def get_table_stats():
    """Get statistics for all tables."""
    try:
        db = SessionLocal()
        inspector = inspect(engine)
        
        logger.info("\n=== Table Statistics ===")
        
        for table_name in inspector.get_table_names():
            try:
                result = db.execute(text(f"SELECT COUNT(*) FROM {table_name}"))
                count = result.scalar()
                logger.info(f"{table_name}: {count} rows")
            except Exception as e:
                logger.warning(f"Could not count {table_name}: {e}")
        
        db.close()
    
    except Exception as e:
        logger.error(f"Error getting table stats: {e}")


def get_database_size():
    """Get database file size."""
    try:
        db_path = Path("memory_weave.db")
        if db_path.exists():
            size_mb = db_path.stat().st_size / (1024 * 1024)
            logger.info(f"Database file size: {size_mb:.2f} MB")
        else:
            logger.warning("Database file not found")
    
    except Exception as e:
        logger.error(f"Error getting database size: {e}")


def main():
    """Main entry point."""
    logger.info("Checking database health...")
    
    if not check_database():
        sys.exit(1)
    
    get_table_stats()
    get_database_size()
    logger.info("\nDatabase health check complete!")


if __name__ == "__main__":
    main()
