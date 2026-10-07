# Project Sharing Checklist

Use this checklist before giving the project to another developer or opening it in another Codex environment.

## Include

- Project source files under `src/`
- Tests under `tests/`
- Project documentation under `docs/`
- `docs/CODEX_HANDOVER.md`
- `.agents/skills/anuradhapura-ai-development/`
- `AGENTS.md`, if present
- Historical dataset: `docs/anuradhapura_weather_2010_2025_era5.csv`
- Trained model artifacts, if the recipient needs to run inference locally: `src/forecasting-service/artifacts/v1_era5_2010_2025/`
- Backend package/project files, including `.csproj` files and `src/backend/AnuradhapuraAI.slnx`
- Frontend package files, including `src/frontend/package.json` and `src/frontend/package-lock.json`
- Forecasting-service dependency/config files, including `src/forecasting-service/pyproject.toml` and `src/forecasting-service/.env.example`
- Database configuration placeholders, not real local credentials

## Exclude Or Remove

- Real passwords
- API keys
- JWT signing keys
- Access tokens
- Database credentials
- Personal authentication tokens
- Machine-specific credentials
- Local secret files such as `.env`, `.env.*`, `secrets.json`, `appsettings.Local.json`, and user-secret exports
- Personal `.codex` directories or personal Codex session files
- IDE/user folders such as `.vs/`, `.vscode/`, and `.idea/` unless intentionally shared as project settings
- Build/cache folders such as `bin/`, `obj/`, `node_modules/`, `.angular/cache/`, `__pycache__/`, and `.pytest_cache/`

## Check Before Sharing

- `docs/CODEX_HANDOVER.md` is present and up to date.
- `.agents/skills/anuradhapura-ai-development/` is present.
- `AGENTS.md` is present if this repository uses it.
- The ERA5 dataset file is available if future work needs training/evaluation context.
- The trained model artifact folder is available if future work needs local inference.
- Package/dependency files are present for backend, frontend, and forecasting-service setup.
- Configuration files contain placeholders only.
- No real secrets or credentials are included.
- No personal Codex session metadata is included.
- No personal authentication tokens are included.
- No machine-specific credentials are included.

## Note About `docs/CODEX_SESSION.md`

`docs/CODEX_SESSION.md` is intended for the original owner's local session recovery. It should not be relied upon as the handover mechanism for another user.

Use `docs/CODEX_HANDOVER.md` as the stable project handover document.
