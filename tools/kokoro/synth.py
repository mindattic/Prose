#!/usr/bin/env python3
"""StreetSamurai local-TTS adapter — Kokoro-82M.

Contract (shared by every local engine adapter):
    python synth.py --text <in.txt> --out <out.wav> [--voice <id>]
Reads UTF-8 text, writes a 24 kHz mono WAV, exits 0. The C# PythonTtsService
resamples the WAV to the pipeline's PCM with ffmpeg, so this only has to make
*a* WAV.

Kokoro is an 82M-parameter open-weight model (Apache-2.0) that runs comfortably
on CPU — the recommended free default for bedtime/draft listens.

One-time setup (from this directory):
    python -m venv .venv
    .venv\\Scripts\\python -m pip install -U kokoro soundfile
    # espeak-ng must be installed for English G2P; on Windows the kokoro wheel
    # bundles a fallback, but installing espeak-ng improves pronunciation.
Then the engine resolves automatically (the C# side prefers tools\\kokoro\\.venv).

Default voice: af_heart. Override with --voice (e.g. am_michael, bm_george) or
the SS_KOKORO_VOICE env var.
"""
import argparse, sys, wave, struct

# On TLS-intercepting networks (corporate proxy / MITM CA), Python's bundled certs
# reject pypi/HuggingFace even though the OS trusts the intercept CA. truststore
# routes SSL through the OS cert store, fixing model downloads. No-op if absent.
try:
    import truststore as _ts
    _ts.inject_into_ssl()
except Exception:
    pass

DEFAULT_VOICE = "af_heart"
SAMPLE_RATE = 24000


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--text", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--voice", default=DEFAULT_VOICE)
    args = ap.parse_args()

    with open(args.text, "r", encoding="utf-8") as fh:
        text = fh.read().strip()
    if not text:
        # Emit a short silence rather than failing the whole audiobook on a blank chunk.
        _write_silence(args.out, 0.2)
        return 0

    try:
        from kokoro import KPipeline
        import numpy as np
    except Exception as e:  # pragma: no cover - setup guidance
        sys.stderr.write(
            "Kokoro not installed. From tools\\kokoro:\n"
            "  python -m venv .venv\n"
            "  .venv\\Scripts\\python -m pip install -U kokoro soundfile\n"
            f"(import error: {e})\n")
        return 3

    # lang_code 'a' = American English; first letter of the voice id also encodes it.
    lang = args.voice[0] if args.voice and args.voice[0] in "ab" else "a"
    pipeline = KPipeline(lang_code=lang)

    chunks = []
    for _, _, audio in pipeline(text, voice=args.voice):
        chunks.append(audio)
    if not chunks:
        _write_silence(args.out, 0.2)
        return 0

    import numpy as np
    audio = np.concatenate(chunks).astype("float32")
    # float [-1,1] -> int16 PCM WAV
    clipped = np.clip(audio, -1.0, 1.0)
    pcm = (clipped * 32767.0).astype("<i2").tobytes()
    with wave.open(args.out, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SAMPLE_RATE)
        w.writeframes(pcm)
    return 0


def _write_silence(path: str, seconds: float) -> None:
    n = int(SAMPLE_RATE * seconds)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SAMPLE_RATE)
        w.writeframes(struct.pack("<" + "h" * n, *([0] * n)))


if __name__ == "__main__":
    sys.exit(main())
