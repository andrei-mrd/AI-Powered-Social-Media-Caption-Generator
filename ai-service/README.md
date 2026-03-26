# AI-Powered Social Media Caption Generator (FastAPI)

FastAPI service that creates platform-optimized captions and hashtags using the OpenAI API. No database or external services beyond OpenAI.

## Setup
- Use the existing virtual environment or create one: `python -m venv .venv`
- Activate it: `source .venv/bin/activate`
- Install deps: `pip install -r requirements.txt`
- Add your API key in `.env`: `OPENAI_API_KEY=...` (see `.env.example`)

## Run
`uvicorn main:app --reload --port 8001`

### Health
- `GET /health` — returns status and whether the OpenAI key is configured (used by the .NET API health probe).

## Endpoint
- `POST /generate-caption`
- Body:
```json
{
  "description": "string",
  "platform": "instagram|tiktok|linkedin",
  "tone": "funny|professional|inspirational",
  "count": 5
}
```
- Returns captions, hashtags (8-15), and metadata. Safety-flagged prompts return `400`.

## Example cURL Request
```bash
curl -X POST http://localhost:8001/generate-caption \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Behind-the-scenes of our eco-friendly packaging line",
    "platform": "instagram",
    "tone": "inspirational",
    "count": 3
  }'
```

### Example Response
```json
{
  "captions": [
    "Sneak peek at how we wrap care into every eco-friendly package—because the planet deserves our best.",
    "From our line to your hands, sustainable choices every step of the way. Ready to unbox greener?",
    "Tiny details, big impact. Proud to ship with materials that respect the earth."
  ],
  "hashtags": [
    "#EcoFriendly", "#SustainablePackaging", "#BehindTheScenes", "#GreenBusiness",
    "#PlanetFirst", "#ConsciousBrand", "#Unboxing", "#EcoChic", "#EarthLovers"
  ],
  "metadata": {
    "platform": "instagram",
    "tone": "inspirational",
    "count": 3
  }
}
```

## Quick CLI Test (curl-only)
- Basic check (replace fields as needed):
```bash
curl -s -X POST http://localhost:8001/generate-caption \
  -H "Content-Type: application/json" \
  -d '{"description":"Productivity tips for remote teams","platform":"linkedin","tone":"professional","count":2}'
```
- For prettier output, pipe to `jq` if you have it installed: `... | jq`

## Notes
- Captions capped at 10 per request; hashtags trimmed to 8-15 and deduplicated.
- Unsafe descriptions (hateful/sexual/violent) are blocked via OpenAI moderation with a 400 response.
