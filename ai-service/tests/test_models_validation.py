import pytest
from pydantic import ValidationError

from models import GenerateCaptionRequest, ImproveCaptionRequest


def test_platform_tone_lowercase():
    req = GenerateCaptionRequest(
        description="Something long enough",
        platform="INSTAGRAM",
        tone="FUNNY",
        count=1,
    )
    assert req.platform == "instagram"
    assert req.tone == "funny"


def test_description_too_short_raises():
    with pytest.raises(ValidationError):
        GenerateCaptionRequest(description="short", platform="instagram", tone="funny", count=1)


def test_hashtag_count_limits():
    with pytest.raises(ValidationError):
        GenerateCaptionRequest(
            description="long enough description",
            platform="instagram",
            tone="funny",
            hashtag_count=50,
        )


def test_clean_lists():
    req = GenerateCaptionRequest(
        description="long enough description",
        platform="instagram",
        tone="funny",
        forbidden_words=[" bad ", " ", ""],
        keywords_to_include=[" a ", "", "b"],
    )
    assert req.forbidden_words == ["bad"]
    assert req.keywords_to_include == ["a", "b"]


def test_improve_caption_requires_text():
    with pytest.raises(ValidationError):
        ImproveCaptionRequest(
            caption=" ",
            platform="instagram",
            tone="funny",
        )


def test_media_urls_dedupe_and_validate():
    req = GenerateCaptionRequest(
        description="Valid description of product",
        platform="instagram",
        tone="funny",
        count=1,
        media_urls=[
            "https://example.com/image.png",
            "https://example.com/image.png",
            "https://example.com/other.jpg",
        ],
    )
    assert len(req.media_urls) == 2
