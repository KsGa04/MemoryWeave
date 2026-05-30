"""Add personality_id to relations table.

Revision ID: 003
Revises: 002
Create Date: 2026-05-30 19:52:00.000000
"""
from alembic import op
import sqlalchemy as sa

revision = "003"
down_revision = "002"
branch_labels = None
depends_on = None


def upgrade() -> None:
    """Add personality_id column to relations table."""
    try:
        op.add_column(
            "relations",
            sa.Column("personality_id", sa.Integer(), nullable=True),
        )
        
        op.execute("UPDATE relations SET personality_id = 1 WHERE personality_id IS NULL")
        op.alter_column("relations", "personality_id", nullable=False)
        
        op.create_foreign_key(
            "fk_relations_personality_id",
            "relations",
            "personalities",
            ["personality_id"],
            ["id"],
            ondelete="CASCADE",
        )
    except Exception:
        pass


def downgrade() -> None:
    """Remove personality_id from relations table."""
    try:
        op.drop_constraint("fk_relations_personality_id", "relations", type_="foreignkey")
        op.drop_column("relations", "personality_id")
    except Exception:
        pass
