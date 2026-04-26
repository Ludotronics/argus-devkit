# Argus CLI

Local command-line tool for bootstrapping Argus in a project and running common QA workflows.

## Install

```bash
pip install -e "path/to/argus-devkit/cli"
```

Requires Python 3.11+.

## Available commands

```bash
argus init
argus verify-determinism
argus run --persona Speedrunner --duration 30m --build latest
argus replay BUG-112
argus bisect BUG-112
argus status
```

## Notes

- `argus init` generates an `argus.plugin.yaml` in the current directory.
- Commands are safe to run locally and do not modify game source automatically.
- Command behavior is intentionally lightweight in this public package version.

## Files

```text
cli/
  pyproject.toml
  src/main.py
```
