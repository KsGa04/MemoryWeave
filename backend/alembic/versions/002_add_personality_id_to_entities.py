"""Add personality_id to entities table.

Revision ID: 002
Revises: 001
Create Date: 2026-05-30 19:51:00.000000
"""
from alembic import op
import sqlalchemy as sa

revision = "002"
down_revision = "001"
branch_labels = None
depends_on = None


def upgrade() -> None:
    """Add personality_id column to entities table."""
    # Check if column already exists
    try:
        op.add_column(
            "entities",
            sa.Column("personality_id", sa.Integer(), nullable=True),
        )
        
        # Set default personality_id = 1 for existing records
        op.execute("UPDATE entities SET personality_id = 1 WHERE personality_id IS NULL")
        
        # Make column NOT NULL
        op.alter_column("entities", "personality_id", nullable=False)
        
        # Add foreign key
        op.create_foreign_key(
            "fk_entities_personality_id",
            "entities",
            "personalities",
            ["personality_id"],
            ["id"],
            ondelete="CASCADE",
        )
    except Exception:
        # Column already exists
        pass


def downgrade() -> None:
    """Remove personality_id from entities table."""
    try:
        op.drop_constraint("fk_entities_personality_id", "entities", type_="foreignkey")
        op.drop_column("entities", "personality_id")
    except Exception:
        pass
