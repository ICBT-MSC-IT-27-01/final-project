"""Reproducible local records for forecasting-model experiments."""

import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path


def append_experiment_record(
    output_dir: Path | str,
    *,
    experiment_id: str,
    model_version: str,
    dataset_sha256: str,
    evaluation_split: str,
    config: dict,
    metrics: dict,
    artifact_directory: Path | str,
) -> Path:
    """Append one immutable JSONL experiment record under the experiment output root."""
    register_path = Path(output_dir) / "experiment_register.jsonl"
    register_path.parent.mkdir(parents=True, exist_ok=True)
    record = {
        "experiment_id": experiment_id,
        "recorded_at_utc": datetime.now(timezone.utc).isoformat(),
        "git_commit": _git_commit(),
        "dataset_sha256": dataset_sha256,
        "evaluation_split": evaluation_split,
        "model_version": model_version,
        "config": config,
        "metrics": metrics,
        "artifact_directory": str(artifact_directory),
    }
    with register_path.open("a", encoding="utf-8") as register_file:
        register_file.write(json.dumps(record, sort_keys=True) + "\n")
    return register_path


def _git_commit() -> str | None:
    try:
        return subprocess.check_output(
            ["git", "rev-parse", "HEAD"],
            text=True,
            stderr=subprocess.DEVNULL,
        ).strip()
    except (OSError, subprocess.CalledProcessError):
        return None
