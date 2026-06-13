# Kokoro local TTS (free)

Free, fully-local narrator for `ss --publish-audiobook --tts kokoro`. Kokoro-82M
(Apache-2.0) runs comfortably on CPU — the recommended free default.

## One-time setup (from this folder)
```
python -m venv .venv
.venv\Scripts\python -m pip install -U kokoro soundfile
```
That's it — the engine auto-resolves (`tools\kokoro\.venv\Scripts\python.exe` +
`tools\kokoro\synth.py`). Verify:
```
.venv\Scripts\python synth.py --text test.txt --out test.wav
```

## Use
```
ss --publish-audiobook --slug <slug> --tts kokoro
```

## Voices
Default `af_heart`. Override per run with `--voice` on synth.py, or set
`SS_KOKORO_VOICE` (e.g. `am_michael`, `bm_george`). Voice id's first letter picks
the language pack (`a`=American, `b`=British English).

## Overrides
- `SS_PYTHON` — python to use instead of the venv.
- `SS_KOKORO_SCRIPT` — adapter path instead of this folder's synth.py.
- `SS_KOKORO_VOICE` — default voice id.
