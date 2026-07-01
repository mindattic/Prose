"""
BERTopic gripe miner.

Trains on 21,936 StrandReviews.Improvements texts to discover recurring
patterns in what readers find weak. Writes per-strand topic Findings when a
topic appears in ≥ GRIPE_TOPIC_MIN_PERCENT of a strand's ballots.
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

import json
import mlflow
import numpy as np
from pathlib import Path
from rich.console import Console
from config import TOPIC_MODEL_PATH, GRIPE_TOPIC_MIN_PERCENT

console = Console()


class GripeMiner:
    def __init__(self, model_path: Path | None = None):
        self.model_path = model_path or TOPIC_MODEL_PATH
        self.topic_model = None

    def train(self, texts: list[str]) -> None:
        from bertopic import BERTopic
        from hdbscan import HDBSCAN
        from sentence_transformers import SentenceTransformer
        from sklearn.feature_extraction.text import CountVectorizer

        console.print(f"[cyan]Training BERTopic on {len(texts):,} gripe texts...[/cyan]")

        embedder = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")
        hdbscan  = HDBSCAN(min_cluster_size=15, min_samples=5, metric="euclidean",
                            cluster_selection_method="eom", prediction_data=True)
        vectorizer = CountVectorizer(stop_words="english", min_df=5, ngram_range=(1, 2))

        self.topic_model = BERTopic(
            embedding_model=embedder,
            hdbscan_model=hdbscan,
            vectorizer_model=vectorizer,
            calculate_probabilities=True,
            verbose=True,
        )
        topics, probs = self.topic_model.fit_transform(texts)

        n_topics   = len(set(topics)) - (1 if -1 in topics else 0)
        n_outliers = int(np.sum(topics == -1))
        console.print(f"[green]{n_topics} topics, {n_outliers} outliers[/green]")

        if mlflow.active_run():
            mlflow.log_metrics({
                "n_topics":   n_topics,
                "n_outliers": n_outliers,
                "n_texts":    len(texts),
            })
            summary = self.get_topic_summary()
            mlflow.log_text(
                json.dumps(summary, indent=2, default=str),
                "gripe_topics.json",
            )

        Path(self.model_path).mkdir(parents=True, exist_ok=True)
        self.topic_model.save(str(self.model_path))

    def load(self) -> None:
        from bertopic import BERTopic
        self.topic_model = BERTopic.load(str(self.model_path))

    def get_topic_summary(self) -> list[dict]:
        if self.topic_model is None:
            return []
        results = []
        for topic_id, words_scores in self.topic_model.get_topics().items():
            if topic_id == -1:
                continue
            info = self.topic_model.get_topic_info(topic_id)
            results.append({
                "id":       topic_id,
                "label":    self.topic_model.get_topic_label(topic_id),
                "keywords": [w for w, _ in words_scores[:8]],
                "size":     int(info["Count"].iloc[0]) if len(info) else 0,
            })
        return sorted(results, key=lambda x: x["size"], reverse=True)

    def topics_for_gripes(self, gripe_texts: list[str]) -> list[int]:
        """Return topic id per text (-1 = outlier)."""
        if self.topic_model is None:
            raise RuntimeError("Call train() or load() first.")
        topics, _ = self.topic_model.transform(gripe_texts)
        return topics

    def strand_findings(self, strand_slug: str, gripe_texts: list[str]) -> list[dict]:
        """
        Return Findings dicts for topics that appear in ≥ GRIPE_TOPIC_MIN_PERCENT
        of this strand's gripes.
        """
        if not gripe_texts:
            return []
        topics = self.topics_for_gripes(gripe_texts)
        total  = len(topics)
        counts: dict[int, int] = {}
        for t in topics:
            if t != -1:
                counts[t] = counts.get(t, 0) + 1

        findings = []
        summary_map = self.get_topic_summary()
        topic_info  = {t["id"]: t for t in summary_map}

        for topic_id, count in counts.items():
            pct = count / total * 100
            if pct < GRIPE_TOPIC_MIN_PERCENT:
                continue
            info     = topic_info.get(topic_id, {})
            label    = info.get("label", f"topic-{topic_id}")
            keywords = ", ".join(info.get("keywords", [])[:5])
            # Find an example gripe for this topic
            example = next(
                (gripe_texts[i] for i, t in enumerate(topics) if t == topic_id),
                ""
            )
            findings.append({
                "file_path":    f"strand:{strand_slug}",
                "severity":     "Medium" if pct >= 30 else "Low",
                "summary":      f"ML-PROSE-GRIPE: Topic '{label}' in {pct:.0f}% of gripes (N={count})",
                "snippet":      None,
                "suggested_fix": f"Keywords: {keywords}. Example: \"{example[:120]}\"",
            })
        return findings
