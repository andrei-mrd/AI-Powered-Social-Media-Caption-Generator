import pytest
from fastapi.testclient import TestClient

import main


class FakeCaptionService:
    def __init__(self, *, mode: str = "ok") -> None:
        self.mode = mode

    def generate_captions(self, payload):
        if self.mode == "value_error":
            raise ValueError("bad request")
        if self.mode == "crash":
            raise RuntimeError("boom")

        captions = [
            {
                "text": f"{payload.platform}:{payload.tone}:{payload.description}:{i}",
                "hook": None,
                "cta": None,
                "hashtags": [f"#tag{i}" for i in range(1, payload.hashtag_count + 1)],
                "score": 80,
                "score_reason": "ok",
            }
            for i in range(1, payload.count + 1)
        ]
        return captions, 0

    def improve_caption(self, payload):
        if self.mode == "value_error":
            raise ValueError("bad request")
        return {
            "improved_caption": f"improved:{payload.caption}",
            "shorter_version": "short",
            "stronger_cta_version": "cta",
        }


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
