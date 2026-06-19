# Kokoro local TTS

Free, fully-local TTS using **Kokoro-82M** (Apache-2.0). Runs comfortably on CPU — the recommended free default for draft and bedtime listens.

`synth.py` implements the shared adapter contract: reads UTF-8 text from a file, writes a 24 kHz mono WAV, exits 0. The C# `PythonTtsService` resamples to pipeline PCM via ffmpeg.

## One-time setup

```powershell
cd tools\kokoro
python -m venv .venv
.venv\Scripts\python -m pip install -U kokoro soundfile
```

Verify:

```powershell
.venv\Scripts\python synth.py --text test.txt --out test.wav
```

The engine auto-resolves (`tools\kokoro\.venv\Scripts\python.exe` + `tools\kokoro\synth.py`). No manual configuration needed.

> **Note:** espeak-ng improves English pronunciation; the kokoro wheel bundles a fallback on Windows if it is not installed.

## Use

```powershell
ss --publish-audiobook --slug <slug> --tts kokoro
```

## Voices

Default: `af_heart`. The first letter of the voice ID selects the language pack:

- `a` = American English (e.g. `af_heart`, `am_michael`)
- `b` = British English (e.g. `bm_george`)

Override per run:

```powershell
.venv\Scripts\python synth.py --text in.txt --out out.wav --voice am_michael
```

Or set `SS_KOKORO_VOICE` for the default used by the C# pipeline.

## Overrides (env vars)

| Variable | Effect |
| --- | --- |
| `SS_PYTHON` | Python executable to use instead of the venv |
| `SS_KOKORO_SCRIPT` | Adapter path instead of this synth.py |
| `SS_KOKORO_VOICE` | Default voice ID |
