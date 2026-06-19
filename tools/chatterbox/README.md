# Chatterbox local TTS

Free, fully-local expressive TTS using Resemble AI's **Chatterbox** model (MIT license). Heavier than Kokoro — a CUDA GPU helps but CPU works for overnight batches.

`synth.py` implements the shared adapter contract: reads UTF-8 text from a file, writes a mono WAV, exits 0. The C# `PythonTtsService` resamples to pipeline PCM via ffmpeg. Long text is automatically split into sentence-sized chunks (≤320 chars by default) and concatenated to avoid Chatterbox's ~40-second generation limit.

## One-time setup

```powershell
cd tools\chatterbox
python -m venv .venv
.venv\Scripts\python -m pip install -U chatterbox-tts soundfile torch
```

Verify:

```powershell
.venv\Scripts\python synth.py --text test.txt --out test.wav
```

The engine auto-resolves (`tools\chatterbox\.venv\Scripts\python.exe` + `tools\chatterbox\synth.py`). No manual configuration needed.

## Use

```powershell
ss --publish-audiobook --slug <slug> --tts chatterbox
```

## Voice cloning (optional)

Pass a short, clean reference WAV to clone a narrator timbre:

- Set `SS_CHATTERBOX_VOICE` to the WAV path, or
- Pass `--voice ref.wav` directly to `synth.py`

Without a reference, the model's built-in voice is used.

## Tuning (env vars)

| Variable | Default | Effect |
| --- | --- | --- |
| `SS_CHATTERBOX_EXAGGERATION` | `0.5` | Lower = calmer, better for bedtime narration |
| `SS_CHATTERBOX_CFG` | `0.5` | CFG weight |
| `SS_CHATTERBOX_CHUNK` | `320` | Max chars per synthesis chunk |
| `SS_PYTHON` | (venv python) | Override python executable |
| `SS_CHATTERBOX_SCRIPT` | (this synth.py) | Override adapter path |
