"""
SetFit beat-mode classifier (Phase 5).

Replaces the keyword-scan in BeatModeDetector.Detect() with a trained
sentence-transformer classifier. Training data comes from BeatModeLog rows
with Confidence > 0.8 joined to Beat.Synopsis (the beat goal text).

After training, the model is saved to BEATMODE_MODEL_PATH and can be
invoked by the C# MlBeatModeService via CLI:
    python beatmode_classifier.py --infer --text "<beat goal>"
Output: {"mode": "Combat", "confidence": 0.92}
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import json
import argparse
import mlflow
from pathlib import Path
from rich.console import Console
from config import BEATMODE_MODEL_PATH

console = Console()

LABEL_MAP = {
    "Narrative": 0, "Combat": 1, "EmotionalClimax": 2,
    "Dialogue": 3, "Transition": 4, "Revelation": 5,
}
LABEL_INV = {v: k for k, v in LABEL_MAP.items()}

BEATMODE_LOG_SQL = """
SELECT
    bml.Mode,
    bml.Confidence,
    b.Synopsis AS BeatGoal
FROM BeatModeLog bml
JOIN Beats b ON b.Id = bml.BeatId
WHERE bml.Confidence > 0.8
  AND bml.DetectionMethod = 'keyword'
  AND b.Synopsis IS NOT NULL
  AND LEN(TRIM(b.Synopsis)) > 5
"""


def pull_training_data(conn) -> tuple[list[str], list[str]]:
    cursor = conn.cursor()
    cursor.execute(BEATMODE_LOG_SQL)
    rows = cursor.fetchall()
    texts  = [r[2].strip() for r in rows]
    labels = [r[0].strip() for r in rows]
    console.print(f"[cyan]{len(texts)} labeled beat goals for mode classifier[/cyan]")
    return texts, labels


class BeatModeClassifier:
    def __init__(self, model_path: Path | None = None):
        self.model_path = model_path or Path(BEATMODE_MODEL_PATH)
        self.model = None

    def train(self, texts: list[str], labels: list[str]) -> None:
        from setfit import SetFitModel, Trainer, TrainingArguments
        from datasets import Dataset

        # Filter to known labels; DB may contain future enum values not in LABEL_MAP
        known = [(t, l) for t, l in zip(texts, labels) if l in LABEL_MAP]
        if len(known) < len(texts):
            console.print(f"[yellow]  Dropped {len(texts) - len(known)} rows with unknown Mode labels[/yellow]")
        if not known:
            console.print("[yellow]No usable labeled data — skipping SetFit training.[/yellow]")
            return
        texts, labels = zip(*known)
        texts, labels = list(texts), list(labels)

        # Build dataset
        label_ids = [LABEL_MAP[l] for l in labels]
        ds = Dataset.from_dict({"text": texts, "label": label_ids})
        ds = ds.train_test_split(test_size=0.15, seed=42)

        console.print("[cyan]Training SetFit beat-mode classifier...[/cyan]")
        self.model = SetFitModel.from_pretrained(
            "sentence-transformers/paraphrase-MiniLM-L6-v2",
            labels=list(LABEL_MAP.keys()),
        )
        args = TrainingArguments(
            batch_size=16, num_epochs=5, seed=42,
        )
        trainer = Trainer(model=self.model, args=args,
                          train_dataset=ds["train"], eval_dataset=ds["test"])
        trainer.train()
        metrics = trainer.evaluate()
        console.print(f"[green]SetFit accuracy: {metrics.get('accuracy', '?'):.3f}[/green]")

        if mlflow.active_run():
            mlflow.log_metric("beatmode_accuracy", metrics.get("accuracy", 0.0))
            mlflow.log_metric("beatmode_train_size", len(ds["train"]))

        self.model_path.mkdir(parents=True, exist_ok=True)
        self.model.save_pretrained(str(self.model_path))

    def load(self) -> None:
        from setfit import SetFitModel
        self.model = SetFitModel.from_pretrained(str(self.model_path))

    def predict(self, texts: list[str]) -> list[tuple[str, float]]:
        """Returns [(mode_label, confidence)] per text."""
        if self.model is None:
            raise RuntimeError("Call train() or load() first.")
        probs = self.model.predict_proba(texts)
        results = []
        for p in probs:
            idx  = int(p.argmax())
            conf = float(p[idx])
            results.append((LABEL_INV[idx], conf))
        return results


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--train", action="store_true")
    parser.add_argument("--infer", action="store_true")
    parser.add_argument("--text", type=str, default="")
    args = parser.parse_args()

    clf = BeatModeClassifier()

    if args.train:
        from db import get_connection
        with get_connection() as conn:
            texts, labels = pull_training_data(conn)
        clf.train(texts, labels)

    if args.infer:
        clf.load()
        if not args.text:
            print(json.dumps({"mode": "Narrative", "confidence": 0.5}))
            return
        preds = clf.predict([args.text])
        mode, conf = preds[0]
        print(json.dumps({"mode": mode, "confidence": round(conf, 4)}))


if __name__ == "__main__":
    main()
