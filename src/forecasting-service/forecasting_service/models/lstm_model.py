from forecasting_service.config.training_config import TrainingConfig


def build_lstm_model(config: TrainingConfig):
    try:
        import tensorflow as tf
    except ImportError as exc:
        raise RuntimeError(
            "TensorFlow is required for LSTM training. Install the 'training' optional dependencies."
        ) from exc

    config.validate()
    model = tf.keras.Sequential(name="anuradhapura_multi_output_lstm")
    for layer_index in range(config.lstm_layers):
        return_sequences = layer_index < config.lstm_layers - 1
        if layer_index == 0:
            model.add(
                tf.keras.layers.Input(
                    shape=(config.lookback_days, len(config.feature_columns))
                )
            )
        model.add(tf.keras.layers.LSTM(config.lstm_units, return_sequences=return_sequences))
        if config.dropout > 0:
            model.add(tf.keras.layers.Dropout(config.dropout))
    model.add(tf.keras.layers.Dense(config.forecast_horizon_days * len(config.feature_columns)))
    model.add(
        tf.keras.layers.Reshape(
            (config.forecast_horizon_days, len(config.feature_columns))
        )
    )
    model.compile(
        optimizer=tf.keras.optimizers.Adam(learning_rate=config.learning_rate),
        loss="mse",
    )
    return model
