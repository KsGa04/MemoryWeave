"""Add personalities table.

Revision ID: 001
Revises: None
Create Date: 2026-05-30 19:50:00.000000
"""
from alembic import op
import sqlalchemy as sa

revision = "001"
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    """Create personalities table."""
    op.create_table(
        "personalities",
        sa.Column("id", sa.Integer(), nullable=False),
        sa.Column("name", sa.String(255), nullable=False, unique=True),
        sa.Column("description", sa.Text(), nullable=True),
        sa.Column("telegram_phone", sa.String(20), nullable=True),
        sa.Column("telegram_api_id", sa.String(255), nullable=True),
        sa.Column("telegram_api_hash", sa.String(255), nullable=True),
        sa.Column("telegram_session_file", sa.String(255), nullable=True),
        sa.Column("obsidian_folder", sa.String(255), nullable=True),
        sa.Column("obsidian_vault_path", sa.String(255), nullable=True),
        sa.Column("style_description", sa.Text(), nullable=True),
        sa.Column("is_active", sa.Boolean(), nullable=False, default=True),
        sa.Column("created_at", sa.DateTime(), nullable=False, server_default=sa.func.now()),
        sa.Column("updated_at", sa.DateTime(), nullable=False, server_default=sa.func.now()),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint("name", name="uq_personalities_name"),
    )


def downgrade() -> None:
    """Drop personalities table."""
    op.drop_table("personalities")
