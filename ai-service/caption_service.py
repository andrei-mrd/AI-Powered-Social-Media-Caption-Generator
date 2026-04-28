import json
import logging
import uuid
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

from openai import OpenAI, OpenAIError

from models import (
    GenerateCaptionRequest,
    ImproveCaptionRequest,
    ImproveCaptionResponse,
    StructuredCaption,
)


PLATFORM_STYLES = {
    "instagram": "Friendly, emoji-friendly, short to medium length with a clear call-to-action.",
    "tiktok": "Open with a hook, punchy, slang acceptable, high energy.",
    "linkedin": "Professional, value-driven, no slang, concise and credible.",
}


logger = logging.getLogger(__name__)


class CaptionService:
    def __init__(self, client: OpenAI, model: str = "gpt-4o-mini") -> None:
        self.client = client
        self.model = model
        self.moderation_model = "omni-moderation-latest"
        self._last_media_cues: Optional[str] = None

    def _moderate(self, description: str) -> None:
        logger.info("moderation:start")
        try:
            moderation = self.client.moderations.create(
                model=self.moderation_model,
                input=description,
            )
        except OpenAIError as exc:
            logger.error("moderation:error %s", exc)
            raise ValueError("Safety check failed. Please try again.") from exc

        result = moderation.results[0]
        if getattr(result, "flagged", False):
            logger.warning("moderation:flagged")
            raise ValueError(
                "The description appears unsafe (hateful, sexual, or violent). "
                "Please provide a safe prompt."
            )
        logger.info("moderation:ok")

    def _build_generation_messages(self, req: GenerateCaptionRequest, media_cues: str | None) -> List[dict]:
        style = PLATFORM_STYLES.get(req.platform, "")
        instructions = (
            "You are an expert social media strategist. "
            "Return JSON only with the key 'captions'. "
            f"Provide exactly {req.count} distinct captions. "
            "Each caption object must include: text, hook (optional), cta (optional), hashtags (list). "
            f"Generate hashtags aligned to the request goal and audience, targeting {req.hashtag_count} unique tags. "
            "Open with a platform-fit hook of 3-7 words. "
            "Use 1-2 vivid, specific details tied to the description. "
            "Vary cadence with a mix of short and medium sentences; avoid repetition. "
            "Use up to 3 emojis only when include_emojis is true. "
            "Align CTA strength to the goal (engagement/sales/awareness) and platform norms. "
            "Prefer niche, long-tail hashtags over generic ones. "
            "Avoid clichés and buzzwords; keep language fresh. "
            "If brand_voice is provided, mirror it with concise stylistic cues. "
            "Respect forbidden_words; rephrase instead of dropping meaning. "
            "Work in keywords_to_include naturally across captions. "
            "Avoid markdown and numbering. Avoid generic filler. Keep output concise and tailored."
        )
        user_prompt = (
            f"Description: {req.description}\n"
            f"Platform: {req.platform} ({style})\n"
            f"Tone: {req.tone}\n"
            f"Language: {req.language}\n"
            f"Audience: {req.audience or 'general'}\n"
            f"Goal: {req.goal}\n"
            f"Caption length: {req.caption_length}\n"
            f"Include emojis: {req.include_emojis}\n"
            f"Include CTA: {req.include_cta}\n"
            f"Brand voice: {req.brand_voice or 'n/a'}\n"
            f"Forbidden words: {', '.join(req.forbidden_words) or 'none'}\n"
            f"Keywords to include: {', '.join(req.keywords_to_include) or 'none'}\n"
            f"Hashtag count: {req.hashtag_count}"
        )
        if media_cues:
            user_prompt += f"\nMedia cues: {media_cues}"
        return [
            {"role": "system", "content": instructions},
            {"role": "user", "content": user_prompt},
        ]

    def _build_improve_messages(self, req: ImproveCaptionRequest) -> List[dict]:
        style = PLATFORM_STYLES.get(req.platform, "")
        instructions = (
            "You rewrite and improve captions. "
            "Return JSON only with keys improved_caption, shorter_version, stronger_cta_version. "
            "Keep platform fit and tone. Avoid markdown."
        )
        user_prompt = (
            f"Caption: {req.caption}\n"
            f"Platform: {req.platform} ({style})\n"
            f"Tone: {req.tone}\n"
            f"Language: {req.language}\n"
            f"Goal: {req.goal}"
        )
        return [
            {"role": "system", "content": instructions},
            {"role": "user", "content": user_prompt},
        ]

    def _clean_hashtags(self, hashtags: Iterable[Any], target: int) -> List[str]:
        cleaned: List[str] = []
        seen = set()
        for tag in hashtags:
            if not isinstance(tag, str):
                continue
            normalized = tag.strip().replace(" ", "")
            if not normalized:
                continue
            if not normalized.startswith("#"):
                normalized = "#" + normalized.lstrip("#")
            key = normalized.lower()
            if key in seen:
                continue
            seen.add(key)
            cleaned.append(normalized)

        if len(cleaned) < 5:
            raise ValueError("Model returned too few hashtags.")

        return cleaned[:target]

    def _parse_caption_item(self, item: Any, hashtag_target: int) -> StructuredCaption | None:
        if not isinstance(item, dict):
            return None

        text = str(item.get("text", "")).strip()
        if not text:
            return None

        hook = item.get("hook")
        cta = item.get("cta")
        hashtags_raw = item.get("hashtags", []) or []
        try:
            hashtags_clean = self._clean_hashtags(hashtags_raw, hashtag_target)
        except ValueError:
            hashtags_clean = []

        return StructuredCaption(
            text=text,
            hook=hook.strip() if isinstance(hook, str) and hook.strip() else None,
            cta=cta.strip() if isinstance(cta, str) and cta.strip() else None,
            hashtags=hashtags_clean,
            score=None,
            score_reason=None,
        )

    def _parse_captions(self, payload: Dict[str, Any], expected: int, hashtag_target: int) -> List[StructuredCaption]:
        captions_raw = payload.get("captions", [])
        if not isinstance(captions_raw, list):
            raise ValueError("Model response missing captions.")

        parsed: List[StructuredCaption] = []
        for item in captions_raw:
            caption = self._parse_caption_item(item, hashtag_target)
            if caption is not None:
                parsed.append(caption)

        filtered = [c for c in parsed if c.text]
        if len(filtered) < expected:
            raise ValueError("Model did not return enough captions. Please try again.")
        return filtered[:expected]

    def _keyword_score_adjustment(self, text: str, keywords: List[str]) -> Tuple[int, str | None]:
        if not keywords:
            return 0, None

        matched = sum(1 for keyword in keywords if keyword.lower() in text.lower())
        if matched:
            return min(10, matched * 3), "Keywords covered"

        return -5, "Missing keywords"

    def _length_score_adjustment(self, text: str, caption_length: str) -> Tuple[int, str | None]:
        word_count = len(text.split())
        if caption_length == "short" and word_count > 30:
            return -5, "Too long for short"
        if caption_length == "long" and word_count < 15:
            return -5, "Too short for long"
        return 0, None

    def _score_caption(self, caption: StructuredCaption, req: GenerateCaptionRequest) -> StructuredCaption:
        score = 60
        reasons: List[str] = []

        if caption.hook:
            score += 10
            reasons.append("Hook present")
        if req.include_cta and caption.cta:
            score += 10
            reasons.append("CTA included")
        if caption.hashtags:
            score += 5
            reasons.append("Hashtags added")

        for adjustment, reason in (
            self._keyword_score_adjustment(caption.text, req.keywords_to_include),
            self._length_score_adjustment(caption.text, req.caption_length),
        ):
            score += adjustment
            if reason:
                reasons.append(reason)

        caption.score = max(0, min(100, score))
        caption.score_reason = "; ".join(reasons) if reasons else "Baseline score"
        return caption

    def _score_captions(self, captions: List[StructuredCaption], req: GenerateCaptionRequest) -> Tuple[List[StructuredCaption], int]:
        try:
            scored = [self._score_caption(caption, req) for caption in captions]
            best_idx = max(range(len(scored)), key=lambda i: scored[i].score or 0)
            return scored, best_idx
        except Exception as exc:
            logger.error("scoring:error %s", exc)
            return captions, 0

    def _extract_media_cues(self, media_urls: Sequence[str], trace_id: Optional[str]) -> str:
        """Optional vision pass to derive visual hooks."""
        if not media_urls:
            return ""

        vision_prompt = (
            "You analyze visual assets for marketing. "
            "Return JSON with key 'cues' (array of 2-4 short, vivid cues about what you see). "
            "Be concrete: colors, objects, mood, setting, notable text/logos."
        )

        content: List[dict] = [{"type": "text", "text": "Extract marketing cues from these visuals."}]
        for url in media_urls[:4]:
            content.append({"type": "image_url", "image_url": {"url": url}})

        try:
            response = self.client.chat.completions.create(
                model=self.model,
                messages=[
                    {"role": "system", "content": vision_prompt},
                    {"role": "user", "content": content},
                ],
                temperature=0.2,
                max_tokens=200,
                response_format={"type": "json_object"},
            )
            payload = json.loads(response.choices[0].message.content)
            cues = payload.get("cues", [])
            cleaned = [str(c).strip() for c in cues if isinstance(c, str) and str(c).strip()]
            self._last_media_cues = "; ".join(cleaned[:4])
            return self._last_media_cues or ""
        except Exception as exc:
            logger.warning("vision:skip trace_id=%s reason=%s", trace_id, exc)
            self._last_media_cues = None
            return ""

    def _request_batch(
        self, req: GenerateCaptionRequest, temperature: float, media_cues: str | None
    ) -> Tuple[List[StructuredCaption], int]:
        logger.info("generate:start temp=%s", temperature)
        messages = self._build_generation_messages(req, media_cues)
        response = self.client.chat.completions.create(
            model=self.model,
            messages=messages,
            temperature=temperature,
            max_tokens=900,
            response_format={"type": "json_object"},
        )

        content = response.choices[0].message.content
        try:
            payload = json.loads(content)
        except json.JSONDecodeError as exc:
            raise ValueError("Model response was not valid JSON.") from exc

        captions = self._parse_captions(payload, req.count, req.hashtag_count)
        captions_scored, best_idx = self._score_captions(captions, req)
        return captions_scored, best_idx

    def generate_captions(
        self, req: GenerateCaptionRequest, trace_id: Optional[str] = None
    ) -> Tuple[List[StructuredCaption], int, Optional[str]]:
        logger.info("request:generate platform=%s tone=%s trace_id=%s", req.platform, req.tone, trace_id or "n/a")
        self._moderate(req.description)
        media_cues = self._extract_media_cues(req.media_urls, trace_id)

        last_error: Exception | None = None
        for temperature in (0.85, 0.45):
            try:
                captions, best_idx = self._request_batch(req, temperature, media_cues)
                logger.info("generate:success temp=%s trace_id=%s", temperature, trace_id or "n/a")
                return captions, best_idx, self._last_media_cues
            except ValueError as exc:
                logger.warning("generate:retry temp=%s reason=%s trace_id=%s", temperature, exc, trace_id or "n/a")
                last_error = exc
                continue

        if last_error:
            raise last_error

        raise ValueError("Caption generation failed. Please try again.")

    def improve_caption(self, req: ImproveCaptionRequest, trace_id: Optional[str] = None) -> ImproveCaptionResponse:
        logger.info("request:improve platform=%s tone=%s trace_id=%s", req.platform, req.tone, trace_id or "n/a")
        messages = self._build_improve_messages(req)
        try:
            response = self.client.chat.completions.create(
                model=self.model,
                messages=messages,
                temperature=0.7,
                max_tokens=500,
                response_format={"type": "json_object"},
            )
        except OpenAIError as exc:
            logger.error("improve:error %s", exc)
            raise ValueError("Caption improve failed.") from exc

        content = response.choices[0].message.content
        try:
            payload = json.loads(content)
        except json.JSONDecodeError as exc:
            raise ValueError("Model response was not valid JSON.") from exc

        improved = str(payload.get("improved_caption", "")).strip()
        shorter = str(payload.get("shorter_version", "")).strip()
        stronger = str(payload.get("stronger_cta_version", "")).strip()

        if not improved:
            raise ValueError("Model did not return an improved caption.")

        return ImproveCaptionResponse(
            improved_caption=improved,
            shorter_version=shorter or improved[:120],
            stronger_cta_version=stronger or improved,
        )
