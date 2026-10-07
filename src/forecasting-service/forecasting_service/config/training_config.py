from dataclasses import asdict, dataclass, field
from pathlib import Path


FEATURE_COLUMNS = ("Rainfall", "Temperature", "Humidity")


@dataclass(frozen=True)
class SplitConfig:
    train_ratio: float = 0.7
    validation_ratio: float = 0.15
    test_ratio: float = 0.15

    def validate(self) -> None:
        total = self.train_ratio + self.validation_ratio + self.test_ratio
        if abs(total - 1.0) > 1e-6:
            raise ValueError("Split ratios must sum to 1.0.")
        if min(self.train_ratio, self.validation_ratio, self.test_ratio) <= 0:
            raise ValueError("Split ratios must be positive.")


@dataclass(frozen=True)
class TrainingConfig:
    dataset_path: Path | None = None
    output_dir: Path = Path("artifacts")
    model_version: str = "v1"
    date_column: str = "Date"
    feature_columns: tuple[str, str, str] = FEATURE_COLUMNS
    lookback_days: int = 30
    forecast_horizon_days: int = 7
    max_interpolation_gap_days: int = 3
    split: SplitConfig = field(default_factory=SplitConfig)
    train_start: str = "2010-01-01"
    train_end: str = "2021-12-31"
    validation_start: str = "2022-01-01"
    validation_end: str = "2023-12-31"
    test_start: str = "2024-01-01"
    test_end: str = "2025-12-31"
    lstm_units: int = 64
    lstm_layers: int = 1
    dropout: float = 0.2
    batch_size: int = 32
    epochs: int = 100
    learning_rate: float = 0.001
    early_stopping_patience: int = 10
    random_seed: int = 42

    def validate(self) -> None:
        if self.lookback_days <= 0:
            raise ValueError("lookback_days must be positive.")
        if self.forecast_horizon_days != 7:
            raise ValueError("forecast_horizon_days must remain 7 for the approved Phase 5 scope.")
        if self.max_interpolation_gap_days < 0:
            raise ValueError("max_interpolation_gap_days cannot be negative.")
        if self.lstm_layers not in (1, 2):
            raise ValueError("lstm_layers must be 1 or 2 for the initial approved architecture.")
        self.split.validate()

    def to_dict(self) -> dict:
        data = asdict(self)
        data["dataset_path"] = str(self.dataset_path) if self.dataset_path else None
        data["output_dir"] = str(self.output_dir)
        return data
