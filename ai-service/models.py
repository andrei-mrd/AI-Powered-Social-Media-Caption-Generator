from typing import List, Literal, Optional

from pydantic import BaseModel, Field, field_validator


class StructuredCaption(BaseModel):
    text: str
    hook: Optional[str] = None
    cta: Optional[str] = None
    hashtags: List[str] = Field(default_factory=list)
    score: Optional[int] = None
    score_reason: Optional[str] = None


class CaptionMetadata(BaseModel):
    platform: Literal["instagram", "tiktok", "linkedin"]
    tone: Literal["funny", "professional", "inspirational"]
    language: Literal["en", "ro"] = "en"
    goal: Literal["engagement", "sales", "awareness"] = "engagement"
    caption_length: Literal["short", "medium", "long"] = "medium"
    include_emojis: bool = True
    include_cta: bool = True
    hashtag_count: int = Field(12, ge=5, le=20)
    audience: Optional[str] = None
    brand_voice: Optional[str] = None
    keywords_to_include: List[str] = Field(default_factory=list)
    forbidden_words: List[str] = Field(default_factory=list)
    count: int = Field(..., ge=1, le=10)


class GenerateCaptionRequest(BaseModel):
    description: str = Field(..., min_length=8)
    platform: Literal["instagram", "tiktok", "linkedin"]
    tone: Literal["funny", "professional", "inspirational"]
    count: int = Field(5, ge=1, le=10)
    language: Literal["en", "ro"] = "en"
    audience: Optional[str] = None
    goal: Literal["engagement", "sales", "awareness"] = "engagement"
    caption_length: Literal["short", "medium", "long"] = "medium"
    include_emojis: bool = True
    include_cta: bool = True
    hashtag_count: int = Field(12, ge=5, le=20)
    brand_voice: Optional[str] = None
    forbidden_words: List[str] = Field(default_factory=list)
    keywords_to_include: List[str] = Field(default_factory=list)

    @field_validator("description")
    @classmethod
    def strip_description(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("Description cannot be empty.")
        if len(cleaned) < 8:
            raise ValueError("Description is too short.")
        return cleaned

    @field_validator("platform", "tone", mode="before")
    @classmethod
    def normalize_lowercase(cls, value: str) -> str:
        return value.lower()

    @field_validator("audience", "brand_voice", mode="before")
    @classmethod
    def strip_optional(cls, value: Optional[str]) -> Optional[str]:
        if value is None:
            return value
        cleaned = value.strip()
        return cleaned or None

    @field_validator("forbidden_words", "keywords_to_include", mode="before")
    @classmethod
    def clean_list(cls, value: Optional[List[str]]) -> List[str]:
        if not value:
            return []
        cleaned: List[str] = []
        for item in value:
            if not isinstance(item, str):
                continue
            stripped = item.strip()
            if stripped:
                cleaned.append(stripped)
        return cleaned


class GenerateCaptionResponse(BaseModel):
    captions: List[StructuredCaption]
    best_caption_index: int
    metadata: CaptionMetadata


class ImproveCaptionRequest(BaseModel):
    caption: str = Field(..., min_length=4)
    platform: Literal["instagram", "tiktok", "linkedin"]
    tone: Literal["funny", "professional", "inspirational"]
    language: Literal["en", "ro"] = "en"
    goal: Literal["engagement", "sales", "awareness"] = "engagement"

    @field_validator("caption")
    @classmethod
    def strip_caption(cls, value: str) -> str:
        cleaned = value.strip()
        if not cleaned:
            raise ValueError("Caption cannot be empty.")
        return cleaned

    @field_validator("platform", "tone", mode="before")
    @classmethod
    def normalize_lowercase(cls, value: str) -> str:
        return value.lower()


class ImproveCaptionResponse(BaseModel):
    improved_caption: str
    shorter_version: str
    stronger_cta_version: str
