import json
from types import SimpleNamespace

import pytest

from caption_service import CaptionService
from models import GenerateCaptionRequest, ImproveCaptionRequest


class FakeModerationResult:
    def __init__(self, flagged: bool = False) -> None:
        self.flagged = flagged


class FakeOpenAIClient:
    def __init__(self, *, responses=None, flagged=False):
        self._responses = responses or []
        self._call_idx = 0
        self.moderations = SimpleNamespace(create=lambda model, input: SimpleNamespace(results=[FakeModerationResult(flagged=flagged)]))
        self.chat = SimpleNamespace(completions=SimpleNamespace(create=self._create_completion))

    def _create_completion(self, **kwargs):
        if self._call_idx >= len(self._responses):
            raise ValueError("no response")
        payload = self._responses[self._call_idx]
        self._call_idx += 1
        return SimpleNamespace(choices=[SimpleNamespace(message=SimpleNamespace(content=payload))])


def test_clean_hashtags_dedupes_and_limits():
    client = FakeOpenAIClient(responses=[])
    sut = CaptionService(client=client)
    out = sut._clean_hashtags(
        [" tag", "#Tag", "test", "  ", "a a", "#test", "x", "y", "z"], target=5
    )
    assert out[0] == "#tag"
    assert len(out) == 5


def test_generate_uses_fallback_second_temperature():
    bad_payload = json.dumps({"captions": []})
    good_payload = json.dumps(
        {"captions": [{"text": "ok", "hashtags": ["tag1", "tag2", "tag3", "tag4", "tag5"]}]}
    )
    client = FakeOpenAIClient(responses=[bad_payload, good_payload])
    sut = CaptionService(client=client)
    req = GenerateCaptionRequest(
        description="Valid description of product",
        platform="instagram",
        tone="funny",
        count=1,
        hashtag_count=5,
    )
    captions, best_idx, cues = sut.generate_captions(req)
    assert len(captions) == 1
    assert best_idx == 0
    assert cues == "" or cues is None


def test_generate_raises_on_moderation_flagged():
    client = FakeOpenAIClient(responses=[], flagged=True)
    sut = CaptionService(client=client)
    req = GenerateCaptionRequest(
        description="Unsafe stuff",
        platform="instagram",
        tone="funny",
        count=1,
    )
    with pytest.raises(ValueError):
        sut.generate_captions(req)


def test_scoring_graceful_when_error():
    client = FakeOpenAIClient(
        responses=[
            json.dumps({"captions": [{"text": "ok", "hashtags": ["tag1", "tag2", "tag3", "tag4", "tag5"]}]})
        ]
    )
    sut = CaptionService(client=client)
    req = GenerateCaptionRequest(
        description="Valid description of product",
        platform="instagram",
        tone="funny",
        count=1,
    )
    captions = [json.loads(client._responses[0])["captions"][0]]
    parsed = sut._parse_captions({"captions": captions}, expected=1, hashtag_target=5)
    # force scoring error by passing invalid req
    scored, idx = sut._score_captions(parsed, req)
    assert scored[0].score is not None
    assert idx == 0


def test_improve_caption_validates_response():
    payload = json.dumps(
        {
            "improved_caption": "better",
            "shorter_version": "short",
            "stronger_cta_version": "cta",
        }
    )
    client = FakeOpenAIClient(responses=[payload])
    sut = CaptionService(client=client)
    req = ImproveCaptionRequest(
        caption="hello world",
        platform="instagram",
        tone="funny",
    )
    resp = sut.improve_caption(req)
    assert resp.improved_caption == "better"
    assert resp.shorter_version == "short"


def test_generate_with_media_uses_cues_once():
    vision_payload = json.dumps({"cues": ["red shoes", "studio lighting"]})
    caption_payload = json.dumps(
        {"captions": [{"text": "ok", "hashtags": ["tag1", "tag2", "tag3", "tag4", "tag5"]}]}
    )
    client = FakeOpenAIClient(responses=[vision_payload, caption_payload])
    sut = CaptionService(client=client)
    req = GenerateCaptionRequest(
        description="Valid description of product",
        platform="instagram",
        tone="funny",
        count=1,
        hashtag_count=5,
        media_urls=["https://example.com/img.png"],
    )
    _captions, best_idx, cues = sut.generate_captions(req, trace_id="trace123")
    assert best_idx == 0
    assert cues and "red shoes" in cues
