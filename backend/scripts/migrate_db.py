#!/usr/bin/env python
"""Migrate database using Alembic."""

import logging
import sys
from pathlib import Path
from alembic.config import Config
from alembic import command

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def run_migrations():
    """Run Alembic migrations."""
    try:
        # Get path to alembic.ini
        backend_dir = Path(__file__).parent.parent
        alembic_cfg_path = backend_dir.parent / "alembic.ini"
        
        if not alembic_cfg_path.exists():
            logger.error(f"alembic.ini not found at {alembic_cfg_path}")
            return False
        
        # Configure Alembic
        config = Config(str(alembic_cfg_path))
        config.set_main_option(
            "script_location",
            str(backend_dir / "alembic")
        )
        
        logger.info("Running Alembic migrations...")
        command.upgrade(config, "head")
        logger.info("Migrations completed successfully")
        return True
    
    except Exception as e:
        logger.error(f"Migration failed: {e}")
        return False


def main():
    """Main entry point."""
    if not run_migrations():
        sys.exit(1)


if __name__ == "__main__":
    main()
