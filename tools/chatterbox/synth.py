#!/usr/bin/env python3
"""StreetSamurai local-TTS adapter — Resemble Chatterbox (Turbo).

Contract (shared by every local engine adapter):
    python synth.py --text <in.txt> --out <out.wav> [--voice <ref.wav>]
Reads UTF-8 text, writes a mono WAV at the model's native rate, exits 0. The C#
PythonTtsService resamples to the pipeline's PCM with ffmpeg.

Chatterbox is Resemble AI's open-weight expressive TTS (MIT). It's heavier than
Kokoro and benefits from a CUDA GPU, but runs on CPU for overnight batches. The
--voice argument, when given, is a path to a short reference WAV to clone the
narrator timbre; without it the model's built-in voice is used.

One-time setup (from this directory):
    python -m venv .venv
    .venv\\Scripts\\python -m pip install -U chatterbox-tts soundfile torch
Then the engine resolves automatically (the C# side prefers tools\\chatterbox\\.venv).

Tuning via env (optional): SS_CHATTERBOX_EXAGGERATION (default 0.5),
SS_CHATTERBOX_CFG (default 0.5). Lower exaggeration = calmer bedtime read.
"""
import argparse, os, sys, wave, struct

# On TLS-intercepting networks (corporate proxy / MITM CA), Python's bundled certs
# reject pypi/HuggingFace even though the OS trusts the intercept CA. truststore
# routes SSL through the OS cert store, fixing model downloads. No-op if absent.
try:
    import truststore as _ts
    _ts.inject_into_ssl()
except Exception:
    pass

FALLBACK_RATE = 24000


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--text", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--voice", default=None, help="optional reference WAV to clone")
    args = ap.parse_args()

    with open(args.text, "r", encoding="utf-8") as fh:
        text = fh.read().strip()
    if not text:
        _write_silence(args.out, 0.2)
        return 0

    try:
        import torch, torchaudio
        from chatterbox.tts import ChatterboxTTS
    except Exception as e:  # pragma: no cover - setup guidance
        sys.stderr.write(
            "Chatterbox not installed. From tools\\chatterbox:\n"
            "  python -m venv .venv\n"
            "  .venv\\Scripts\\python -m pip install -U chatterbox-tts soundfile torch\n"
            f"(import error: {e})\n")
        return 3

    device = "cuda" if torch.cuda.is_available() else "cpu"
    model = ChatterboxTTS.from_pretrained(device=device)

    exaggeration = float(os.environ.get("SS_CHATTERBOX_EXAGGERATION", "0.5"))
    cfg = float(os.environ.get("SS_CHATTERBOX_CFG", "0.5"))

    kwargs = {"exaggeration": exaggeration, "cfg_weight": cfg}
    if args.voice and os.path.exists(args.voice):
        kwargs["audio_prompt_path"] = args.voice

    # Chatterbox generates ~1000 tokens (~40s) per call, so a whole audiobook
    # segment must be split into sentence-sized chunks and concatenated. Without
    # this, a multi-paragraph chunk silently truncates or errors.
    import re
    chunks = _chunk_text(text, max_chars=int(os.environ.get("SS_CHATTERBOX_CHUNK", "320")))
    pieces = []
    gap = torch.zeros(1, int(model.sr * 0.25))  # 250ms between chunks
    for i, c in enumerate(chunks):
        sys.stderr.write(f"[chatterbox] chunk {i+1}/{len(chunks)} ({len(c)} chars)\n")
        sys.stderr.flush()
        wav = model.generate(c, **kwargs)  # [1, N] float
        pieces.append(wav.cpu())
        pieces.append(gap)
    out = torch.cat(pieces, dim=1) if pieces else torch.zeros(1, int(model.sr * 0.2))
    torchaudio.save(args.out, out, model.sr)
    return 0


def _chunk_text(text, max_chars=320):
    """Split prose into <=max_chars chunks on sentence boundaries, never mid-word."""
    import re
    sentences = re.split(r"(?<=[.!?—])\s+", text.replace("\n", " ").strip())
    chunks, cur = [], ""
    for s in sentences:
        s = s.strip()
        if not s:
            continue
        if len(s) > max_chars:
            # hard-wrap an over-long sentence on word boundaries
            if cur:
                chunks.append(cur); cur = ""
            words, line = s.split(), ""
            for w in words:
                if len(line) + len(w) + 1 > max_chars:
                    chunks.append(line); line = w
                else:
                    line = (line + " " + w).strip()
            if line:
                cur = line
        elif len(cur) + len(s) + 1 > max_chars:
            chunks.append(cur); cur = s
        else:
            cur = (cur + " " + s).strip()
    if cur:
        chunks.append(cur)
    return chunks


def _write_silence(path: str, seconds: float) -> None:
    n = int(FALLBACK_RATE * seconds)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(FALLBACK_RATE)
        w.writeframes(struct.pack("<" + "h" * n, *([0] * n)))


if __name__ == "__main__":
    sys.exit(main())
