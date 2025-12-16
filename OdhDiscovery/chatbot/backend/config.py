"""
Configuration module for ODH Chatbot
Loads settings from environment variables with sensible defaults
"""
from pydantic_settings import BaseSettings
from pydantic import Field
from typing import Literal


class Settings(BaseSettings):
    """Application settings loaded from environment variables"""

    # LLM Provider Configuration (provider-agnostic)
    llm_provider: Literal["togetherai", "openai", "anthropic", "custom"] = Field(
        default="togetherai",
        description="LLM provider to use"
    )
    llm_model: str = Field(
        default="meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo",
        description="Model identifier for the LLM provider"
    )
    llm_api_key: str = Field(
        default="",
        description="API key for LLM provider"
    )
    llm_temperature: float = Field(
        default=0.1,
        ge=0.0,
        le=2.0,
        description="Temperature for LLM responses"
    )
    llm_max_tokens: int = Field(
        default=4096,
        gt=0,
        description="Maximum tokens for LLM responses"
    )
    llm_base_url: str | None = Field(
        default=None,
        description="Custom base URL for LLM API (for custom providers)"
    )

    # API Configuration
    content_api_base_url: str = Field(
        default="http://localhost:5000/api/v1/content",
        description="Base URL for Content API"
    )
    timeseries_api_base_url: str = Field(
        default="http://localhost:5000/api/v1/timeseries",
        description="Base URL for Timeseries API"
    )
    api_timeout: int = Field(
        default=30,
        gt=0,
        description="API request timeout in seconds"
    )

    # Vector Store Configuration
    chroma_host: str = Field(
        default="localhost",
        description="ChromaDB host"
    )
    chroma_port: int = Field(
        default=8000,
        gt=0,
        description="ChromaDB port"
    )
    chroma_collection: str = Field(
        default="odh_docs",
        description="ChromaDB collection name"
    )

    # Backend Configuration
    backend_port: int = Field(
        default=8001,
        gt=0,
        description="Backend server port"
    )
    backend_host: str = Field(
        default="0.0.0.0",
        description="Backend server host"
    )
    log_level: Literal["DEBUG", "INFO", "WARNING", "ERROR"] = Field(
        default="INFO",
        description="Logging level"
    )
    enable_cors: bool = Field(
        default=True,
        description="Enable CORS for frontend integration"
    )

    # Preprocessing Configuration
    max_tokens_per_tool: int = Field(
        default=2000,
        gt=0,
        description="Maximum tokens per tool response"
    )
    enable_auto_aggregation: bool = Field(
        default=True,
        description="Enable automatic data aggregation for large responses"
    )
    default_page_size: int = Field(
        default=50,
        gt=0,
        description="Default page size for paginated API calls"
    )
    max_page_size: int = Field(
        default=200,
        gt=0,
        description="Maximum allowed page size"
    )

    # Authentication Configuration
    auth_username: str = Field(
        default="admin",
        description="Username for authentication"
    )
    auth_password: str = Field(
        default="changeme",
        description="Password for authentication"
    )
    jwt_secret_key: str = Field(
        default="your-secret-key-change-in-production",
        description="Secret key for JWT token signing"
    )
    jwt_algorithm: str = Field(
        default="HS256",
        description="Algorithm for JWT signing"
    )
    jwt_expire_minutes: int = Field(
        default=1440,
        gt=0,
        description="JWT token expiration time in minutes (default: 24 hours)"
    )

    class Config:
        # Look for .env in parent directory (chatbot/.env) and also in current directory
        env_file = "../.env"
        env_file_encoding = "utf-8"
        case_sensitive = False
        extra = "ignore"  # Ignore extra env vars


# Global settings instance
settings = Settings()


def get_llm_config() -> dict:
    """Get LLM configuration based on provider"""
    base_config = {
        "model": settings.llm_model,
        "temperature": settings.llm_temperature,
        "max_tokens": settings.llm_max_tokens,
    }

    if settings.llm_provider == "togetherai":
        return {
            **base_config,
            "api_key": settings.llm_api_key,
            "base_url": settings.llm_base_url or "https://api.together.xyz/v1",
        }
    elif settings.llm_provider == "openai":
        return {
            **base_config,
            "api_key": settings.llm_api_key,
            "base_url": settings.llm_base_url,
        }
    elif settings.llm_provider == "anthropic":
        return {
            **base_config,
            "api_key": settings.llm_api_key,
        }
    elif settings.llm_provider == "custom":
        return {
            **base_config,
            "api_key": settings.llm_api_key,
            "base_url": settings.llm_base_url,
        }

    raise ValueError(f"Unsupported LLM provider: {settings.llm_provider}")


def get_chroma_url() -> str:
    """Get ChromaDB connection URL"""
    return f"http://{settings.chroma_host}:{settings.chroma_port}"
