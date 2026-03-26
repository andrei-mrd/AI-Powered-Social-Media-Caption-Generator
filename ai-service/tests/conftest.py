import os
import sys

import pytest
from fastapi.testclient import TestClient

# Ensure the ai-service root is importable when running pytest from parent directories.
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

import main
from models import ImproveCaptionResponse, StructuredCaption


class FakeCaptionService:
    def __init__(self, *, mode: str = "ok") -> None:
        self.mode = mode

    def generate_captions(self, payload, trace_id=None):
        if self.mode == "value_error":
            raise ValueError("bad request")
        if self.mode == "crash":
            raise RuntimeError("boom")

        captions = [
            StructuredCaption(
                text=f"{payload.platform}:{payload.tone}:{payload.description}:{i}",
                hook=None,
                cta=None,
                hashtags=[f"#tag{i}" for i in range(1, payload.hashtag_count + 1)],
                score=80,
                score_reason="ok",
            )
            for i in range(1, payload.count + 1)
        ]
        return captions, 0, None

    def improve_caption(self, payload, trace_id=None):
        if self.mode == "value_error":
            raise ValueError("bad request")
        return ImproveCaptionResponse(
            improved_caption=f"improved:{payload.caption}",
            shorter_version="short",
            stronger_cta_version="cta",
            trace_id=trace_id,
        )


@pytest.fixture
def client():
    app = main.create_app()
    app.dependency_overrides[main.get_service] = lambda: FakeCaptionService(mode="ok")
    with TestClient(app) as c:
        yield c


@pytest.fixture
def client_value_error():
    app = main.create_app()
    app.dependency_overrides[main.get_service] = lambda: FakeCaptionService(mode="value_error")
    with TestClient(app) as c:
        yield c


@pytest.fixture
def client_crash():
    app = main.create_app()
    app.dependency_overrides[main.get_service] = lambda: FakeCaptionService(mode="crash")
    with TestClient(app) as c:
        yield c
