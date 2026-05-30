"""Add personality_id to events table.

Revision ID: 004
Revises: 003
Create Date: 2026-05-30 19:53:00.000000
"""
from alembic import op
import sqlalchemy as sa

revision = "004"
down_revision = "003"
branch_labels = None
depends_on = None


def upgrade() -> None:
    """Add personality_id column to events table."""
    try:
        op.add_column(
            "events",
            sa.Column("personality_id", sa.Integer(), nullable=True),
        )
        
        op.execute("UPDATE events SET personality_id = 1 WHERE personality_id IS NULL")
        op.alter_column("events", "personality_id", nullable=False)
        
        op.create_foreign_key(
            "fk_events_personality_id",
            "events",
            "personalities",
            ["personality_id"],
            ["id"],
            ondelete="CASCADE",
        )
    except Exception:
        pass


def downgrade() -> None:
    """Remove personality_id from events table."""
    try:
        op.drop_constraint("fk_events_personality_id", "events", type_="foreignkey")
        op.drop_column("events", "personality_id")
    except Exception:
        pass
