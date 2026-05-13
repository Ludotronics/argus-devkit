# Human-like Unity runner (Editor Test mode)

The Argus Unity SDK `StateStreamer` opens a **WebSocket client** to `ws://127.0.0.1:7777/sdk/state`.
This process implements the matching **WebSocket server** so Play Mode can connect.

## Setup

For the Unity developer handoff, prefer the repo-root wrapper. It creates the virtualenv, reads `Turn_Based_Strategy_Game/Assets/Argus.local.json`, runs the WebSocket server, ingests the report, and prints/opens the dashboard URL:

```bash
python3 scripts/run_unity_qa.py --scenario first_turn_smoke --open-dashboard
```

Manual runner setup:

```bash
cd argus-devkit/runner
python3 -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
```

Start **this server first**, then enter Unity Editor Play Mode with Argus **Test** mode enabled.

## Run

```bash
python3 human_like_runner.py --scenario first_turn_smoke --persona newcomer --max-steps 40
```

Scenarios: `first_turn_smoke`, `soak`, `invalid_stress`, `exploratory`, `card_placement`, `pawn_movement`, `pawn_attack`, `game_over_path`, `ten_turn_soak`.

## Backend ingest

After a session, the runner writes `human_like_report.json`. Post it to the API (or use the dashboard **Human-like** panel):

`POST /v1/projects/{project_id}/runs/human-like-session`

Environment for auto-ingest from the runner:

- `ARGUS_API_URL` — e.g. `http://localhost:8000`
- `ARGUS_TOKEN` — optional bearer JWT
- `ARGUS_SDK_KEY` — project SDK key; used to mint a Test-mode session token when `ARGUS_TOKEN` is absent
- `ARGUS_PROJECT_ID` — project UUID
- `ARGUS_BUILD_ID` — optional; otherwise first ready build is used
- `ARGUS_DASHBOARD_URL` — optional; printed as `/runs/{run_id}` after ingest
- `ARGUS_OPEN_DASHBOARD=1` — optional; opens the run URL after ingest

```bash
export ARGUS_API_URL=http://localhost:8000
export ARGUS_TOKEN=...
export ARGUS_PROJECT_ID=...
python3 human_like_runner.py --scenario soak --ingest
```
