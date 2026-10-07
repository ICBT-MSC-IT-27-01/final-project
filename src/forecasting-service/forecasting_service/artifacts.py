import json
import platform
from datetime import datetime, timezone
from importlib.metadata import PackageNotFoundError, version
from pathlib import Path


def build_metadata(
    *,
    model_version: str,
    dataset_path: str | None,
    dataset_profile: dict,
    split_metadata: dict,
    config: dict,
    preprocessing_metadata: dict,
    baseline_metrics: dict | None,
    lstm_metrics: dict | None,
    artifact_paths: dict | None = None,
    sequence_metadata: dict | None = None,
) -> dict:
    return {
        "model_version": model_version,
        "training_timestamp_utc": datetime.now(timezone.utc).isoformat(),
        "python_version": platform.python_version(),
        "package_versions": _package_versions(["numpy", "pandas", "scikit-learn", "tensorflow", "keras"]),
        "dataset_path": dataset_path,
        "dataset_profile": dataset_profile,
        "split_metadata": split_metadata,
        "config": config,
        "preprocessing_metadata": preprocessing_metadata,
        "baseline_metrics": baseline_metrics,
        "lstm_metrics": lstm_metrics,
        "artifact_paths": artifact_paths or {},
        "sequence_metadata": sequence_metadata or {},
    }


def save_metadata(metadata: dict, output_path: Path | str) -> None:
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(metadata, indent=2), encoding="utf-8")


def _package_versions(package_names: list[str]) -> dict[str, str | None]:
    versions: dict[str, str | None] = {}
    for package in package_names:
        try:
            versions[package] = version(package)
        except PackageNotFoundError:
            versions[package] = None
    return versions
