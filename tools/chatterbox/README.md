# Chatterbox (Turbo) local TTS (free)

Free, fully-local expressive narrator for `ss --publish-audiobook --tts chatterbox`.
Resemble AI's Chatterbox (MIT). Heavier than Kokoro; a CUDA GPU helps but CPU works
for overnight batches.

## One-time setup (from this folder)
```
python -m venv .venv
.venv\Scripts\python -m pip install -U chatterbox-tts soundfile torch
```
The engine auto-resolves (`tools\chatterbox\.venv\Scripts\python.exe` +
`tools\chatterbox\synth.py`). Verify:
```
.venv\Scripts\python synth.py --text test.txt --out test.wav
```

## Use
```
ss --publish-audiobook --slug <slug> --tts chatterbox
```

## Voice cloning (optional)
Pass a short clean reference WAV to clone a narrator timbre: set `SS_CHATTERBOX_VOICE`
to its path, or `--voice ref.wav` on synth.py. Without it, the built-in voice is used.

## Tuning (env)
- `SS_CHATTERBOX_EXAGGERATION` (default 0.5) — lower = calmer, better for bedtime.
- `SS_CHATTERBOX_CFG` (default 0.5).
- `SS_PYTHON` / `SS_CHATTERBOX_SCRIPT` — override python / adapter path.
