def test_generate_caption_ok(client):
    resp = client.post(
        "/generate-caption",
        json={
            "description": "hello world long enough",
            "platform": "instagram",
            "tone": "funny",
            "count": 3,
            "hashtag_count": 6,
        },
    )
    assert resp.status_code == 200
    data = resp.json()
    assert data["metadata"]["platform"] == "instagram"
    assert data["metadata"]["tone"] == "funny"
    assert data["metadata"]["count"] == 3
    assert "engagement_score" in data["metadata"]
    assert "engagement_rationale" in data["metadata"]
    assert data.get("trace_id")
    assert len(data["captions"]) == 3
    assert data["best_caption_index"] == 0
    assert all("text" in c for c in data["captions"])


def test_generate_caption_validation_422(client):
    resp = client.post(
        "/generate-caption",
        json={
            "description": "hello",
            "platform": "instagram",
            # tone missing
            "count": 3,
            "language": "en",
        },
    )
    assert resp.status_code == 422


def test_generate_caption_value_error_400(client_value_error):
    resp = client_value_error.post(
        "/generate-caption",
        json={
            "description": "hello world long enough",
            "platform": "instagram",
            "tone": "funny",
            "count": 3,
            "hashtag_count": 6,
        },
    )
    assert resp.status_code == 400


def test_generate_caption_unhandled_500(client_crash):
    resp = client_crash.post(
        "/generate-caption",
        json={
            "description": "hello world long enough",
            "platform": "instagram",
            "tone": "funny",
            "count": 3,
            "hashtag_count": 6,
        },
    )
    assert resp.status_code == 500


def test_improve_caption_ok(client):
    resp = client.post(
        "/improve-caption",
        json={
            "caption": "test caption",
            "platform": "instagram",
            "tone": "funny",
            "language": "en",
            "goal": "engagement",
        },
    )
    assert resp.status_code == 200
    data = resp.json()
    assert data["improved_caption"].startswith("improved:")
    assert data["shorter_version"]
    assert data["stronger_cta_version"]
    assert data.get("trace_id")


def test_improve_caption_validation(client):
    resp = client.post(
        "/improve-caption",
        json={
            "caption": "   ",
            "platform": "instagram",
            "tone": "funny",
        },
    )
    assert resp.status_code == 422
