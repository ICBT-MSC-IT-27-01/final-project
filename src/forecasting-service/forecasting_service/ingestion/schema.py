from dataclasses import dataclass


REQUIRED_COLUMNS = ("Date", "Rainfall", "Temperature", "Humidity")


@dataclass(frozen=True)
class ColumnMapping:
    date: str = "Date"
    rainfall: str = "Rainfall"
    temperature: str = "Temperature"
    humidity: str = "Humidity"
    min_temperature: str | None = None
    max_temperature: str | None = None

    def as_source_columns(self) -> tuple[str, ...]:
        columns = [self.date, self.rainfall, self.humidity]
        if self.temperature:
            columns.append(self.temperature)
        elif self.min_temperature and self.max_temperature:
            columns.extend([self.min_temperature, self.max_temperature])
        return tuple(columns)
