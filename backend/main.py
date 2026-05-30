"""Main FastAPI application."""

import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from backend.app.api.personalities import router as personalities_router
from backend.app.api.health import router as health_router
from backend.app.db.database import init_db

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


# Lifespan context manager
@asynccontextmanager
async def lifespan(app: FastAPI):
    """Handle startup and shutdown events."""
    # Startup
    logger.info("Starting MemoryWeave API...")
    try:
        init_db()
        logger.info("Database initialized successfully")
    except Exception as e:
        logger.error(f"Failed to initialize database: {e}")
        raise
    
    yield
    
    # Shutdown
    logger.info("Shutting down MemoryWeave API...")


# Create FastAPI app
app = FastAPI(
    title="MemoryWeave API",
    description="Personal information system with adaptive dialog agent, long-term memory and semantic network building",
    version="1.0.0",
    lifespan=lifespan
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Adjust for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routers
app.include_router(health_router)
app.include_router(personalities_router)

# Root endpoint
@app.get("/")
async def root():
    """Root endpoint."""
    return {
        "name": "MemoryWeave API",
        "version": "1.0.0",
        "description": "Personal information system with adaptive dialog agent",
        "docs": "/docs",
        "endpoints": {
            "health": "/api/health",
            "stats": "/api/stats",
            "personalities": "/api/personalities"
        }
    }


if __name__ == "__main__":
    import uvicorn
    
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info"
    )
