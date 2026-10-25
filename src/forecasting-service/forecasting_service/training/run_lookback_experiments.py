"""Run the approved validation-only lookback study for the weather LSTM."""

import argparse
from pathlib import Path

from forecasting_service.config.training_config import TrainingConfig
from forecasting_service.training.train import train_from_config


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", required=True, type=Path)
    parser.add_argument("--output-root", type=Path, default=Path("artifacts/model-improvement"))
    parser.add_argument("--lookbacks", type=int, nargs="+", default=[14, 30, 60, 90])
    parser.add_argument("--rainfall-transform", choices=["none", "log1p"], default="none")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    for lookback_days in args.lookbacks:
        transform_suffix = "" if args.rainfall_transform == "none" else f"_{args.rainfall_transform}"
        model_version = f"lookback_{lookback_days}d{transform_suffix}_validation"
        output_dir = args.output_root / model_version
        if output_dir.exists():
            raise FileExistsError(f"Experiment output already exists: {output_dir}")
        config = TrainingConfig(
            dataset_path=args.dataset,
            output_dir=output_dir,
            model_version=model_version,
            lookback_days=lookback_days,
            rainfall_transform=args.rainfall_transform,
        )
        metadata = train_from_config(config, evaluation_split="validation")
        metrics = metadata["lstm_metrics"]
        print(
            f"{model_version}: validation MAE={metrics['overall_mae']:.6f}, "
            f"RMSE={metrics['overall_rmse']:.6f}"
        )


if __name__ == "__main__":
    main()
