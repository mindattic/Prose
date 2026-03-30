import os

import anthropic
from dotenv import load_dotenv


_client = None


def _get_client() -> anthropic.Anthropic:
    global _client
    if _client is None:
        load_dotenv()
        api_key = os.environ.get("ANTHROPIC_API_KEY", "").strip()
        if not api_key:
            raise RuntimeError("Missing ANTHROPIC_API_KEY. Put it in your .env file or environment.")
        _client = anthropic.Anthropic(api_key=api_key)
    return _client


def generate(
    system: str,
    user: str,
    model: str = "claude-sonnet-4-6",
    temperature: float = 0.8,
    max_tokens: int = 4096,
) -> str:
    client = _get_client()
    resp = client.messages.create(
        model=model,
        max_tokens=max_tokens,
        temperature=temperature,
        system=system,
        messages=[{"role": "user", "content": user}],
    )
    return (resp.content[0].text or "").strip()
