import os
import uuid
from typing import Annotated

from dotenv import load_dotenv
from fastapi import Depends, FastAPI, HTTPException, Request
from openai import OpenAI, OpenAIError

from caption_service import CaptionService
from models import (
    CaptionMetadata,
    GenerateCaptionRequest,
    GenerateCaptionResponse,
    ImproveCaptionRequest,
    ImproveCaptionResponse,
)

load_dotenv()

_service_singleton: CaptionService | None = None


def get_service() -> CaptionService:
    global _service_singleton
    if _service_singleton is not None:
        return _service_singleton

    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        raise RuntimeError("OPENAI_API_KEY is not set. Add it to your environment or .env file.")

    client = OpenAI(api_key=api_key)
    _service_singleton = CaptionService(client)
    return _service_singleton


def create_app() -> FastAPI:
    app = FastAPI(
        title="AI-Powered Social Media Caption Generator",
        version="0.1.0",
    )

    @app.middleware("http")
    async def add_trace_id(request: Request, call_next):
        trace_id = request.headers.get("x-request-id") or uuid.uuid4().hex
        request.state.trace_id = trace_id
        response = await call_next(request)
        response.headers["x-request-id"] = trace_id
        return response

    @app.get("/health")
    def health() -> dict:
        """Liveness/readiness probe for orchestrators."""
        api_key_present = bool(os.getenv("OPENAI_API_KEY"))
        return {
            "status": "ok" if api_key_present else "degraded",
            "openai_key": "configured" if api_key_present else "missing",
            "model": get_service().model if api_key_present else None,
        }

    @app.post("/generate-caption")
    def generate_caption(
        payload: GenerateCaptionRequest,
        service: Annotated[CaptionService, Depends(get_service)],
        request: Request,
    ) -> GenerateCaptionResponse:
        try:
            captions, best_idx, media_cues = service.generate_captions(payload, trace_id=request.state.trace_id)
        except ValueError as exc:
            raise HTTPException(status_code=400, detail=str(exc))
        except OpenAIError as exc:
            raise HTTPException(status_code=502, detail=f"OpenAI API error: {exc}")
        except Exception as exc:
            raise HTTPException(status_code=500, detail="Unexpected server error.") from exc

        metadata = CaptionMetadata(
            platform=payload.platform,
            tone=payload.tone,
            language=payload.language,
            goal=payload.goal,
            caption_length=payload.caption_length,
            include_emojis=payload.include_emojis,
            include_cta=payload.include_cta,
            hashtag_count=payload.hashtag_count,
            audience=payload.audience,
            brand_voice=payload.brand_voice,
            keywords_to_include=payload.keywords_to_include,
            forbidden_words=payload.forbidden_words,
            count=payload.count,
            media_urls=payload.media_urls,
            media_cues=media_cues,
            engagement_score=captions[best_idx].score if captions else None,
            engagement_rationale=captions[best_idx].score_reason if captions else None,
            trace_id=request.state.trace_id,
        )

        return GenerateCaptionResponse(
            captions=captions,
            best_caption_index=best_idx,
            metadata=metadata,
            trace_id=request.state.trace_id,
        )

    @app.post("/improve-caption")
    def improve_caption(
        payload: ImproveCaptionRequest,
        service: Annotated[CaptionService, Depends(get_service)],
        request: Request,
    ) -> ImproveCaptionResponse:
        try:
            resp = service.improve_caption(payload, trace_id=request.state.trace_id)
            resp.trace_id = request.state.trace_id
            return resp
        except ValueError as exc:
            raise HTTPException(status_code=400, detail=str(exc))
        except OpenAIError as exc:
            raise HTTPException(status_code=502, detail=f"OpenAI API error: {exc}")
        except Exception as exc:
            raise HTTPException(status_code=500, detail="Unexpected server error.") from exc

    return app


app = create_app()
