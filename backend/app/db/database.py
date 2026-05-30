"""Database connection and session management."""

import logging
from pathlib import Path
from typing import Generator

from sqlalchemy import create_engine, event, text
from sqlalchemy.orm import declarative_base, sessionmaker

logger = logging.getLogger(__name__)

# Database path - create data directory if it doesn't exist
DB_DIR = Path(__file__).parent.parent.parent / "data"
DB_DIR.mkdir(exist_ok=True)
DB_PATH = DB_DIR / "memory.db"

# Database URL
DATABASE_URL = f"sqlite:///{DB_PATH}"

logger.info(f"Database URL: {DATABASE_URL}")

# Create engine with SQLite-specific settings
engine = create_engine(
    DATABASE_URL,
    connect_args={"check_same_thread": False},
    echo=False,  # Set to True for SQL query logging
)

# Enable foreign keys for SQLite
@event.listens_for(engine, "connect")
def set_sqlite_pragma(dbapi_conn, connection_record):
    cursor = dbapi_conn.cursor()
    cursor.execute("PRAGMA foreign_keys=ON")
    cursor.close()

# Create session factory
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Base class for ORM models
Base = declarative_base()


def get_db() -> Generator:
    """Dependency for FastAPI to get database session."""
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def init_db() -> None:
    """Initialize database schema from SQL script and create ORM tables."""
    logger.info("Initializing database...")
    
    # Read schema.sql
    schema_path = Path(__file__).parent / "schema.sql"
    
    if not schema_path.exists():
        logger.error(f"Schema file not found: {schema_path}")
        raise FileNotFoundError(f"Schema file not found: {schema_path}")
    
    with open(schema_path, "r", encoding="utf-8") as f:
        schema_sql = f.read()
    
    # Execute schema script
    with engine.connect() as connection:
        # Split by semicolon and execute statements
        statements = [
            stmt.strip() 
            for stmt in schema_sql.split(";") 
            if stmt.strip() and not stmt.strip().startswith("--")
        ]
        
        for statement in statements:
            try:
                connection.execute(text(statement))
            except Exception as e:
                # Log but don't fail if table already exists
                if "already exists" not in str(e).lower():
                    logger.warning(f"Statement warning: {e}")
        
        connection.commit()
    
    logger.info("Database schema initialized successfully")
    
    # Import models to ensure they're registered with the Base
    from backend.app.models import (
        Personality, Entity, Relation, Message, Event,
        StylePattern, ConversationSession, ConversationMessage,
        ObsidianNote, SyncLog, Configuration, DeduplicationCache
    )
    
    # Print database info
    with SessionLocal() as session:
        tables = session.execute(
            text("SELECT name FROM sqlite_master WHERE type='table'")
        ).fetchall()
        logger.info(f"Available tables: {[t[0] for t in tables]}")
        
        # Count personalities if table exists
        try:
            personality_count = session.execute(
                text("SELECT COUNT(*) FROM personalities")
            ).scalar()
            logger.info(f"Total personalities in database: {personality_count}")
        except Exception as e:
            logger.debug(f"Could not count personalities: {e}")


def drop_db() -> None:
    """Drop all tables (use with caution!)."""
    logger.warning("Dropping all database tables...")
    Base.metadata.drop_all(bind=engine)
    logger.info("Database dropped")


def get_db_stats() -> dict:
    """Get database statistics including personality info."""
    with SessionLocal() as session:
        stats = {}
        
        # Count tables
        tables = session.execute(
            text("SELECT name FROM sqlite_master WHERE type='table'")
        ).fetchall()
        stats["total_tables"] = len(tables)
        
        # Count records in each table
        for table_name, in tables:
            if not table_name.startswith("sqlite_"):
                try:
                    count = session.execute(
                        text(f"SELECT COUNT(*) FROM {table_name}")
                    ).scalar()
                    stats[f"{table_name}_count"] = count
                except Exception as e:
                    logger.debug(f"Could not count {table_name}: {e}")
        
        # Database file size
        if DB_PATH.exists():
            stats["db_file_size_mb"] = round(DB_PATH.stat().st_size / (1024 * 1024), 2)
        
        return stats
