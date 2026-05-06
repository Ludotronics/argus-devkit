# SDK Release Checklist

Use this checklist before publishing Unity, Unreal, Godot, Python, and CLI artifacts.

## Contract and compatibility

- [ ] `common/protocol/schemas/` unchanged or migration notes documented.
- [ ] Backend accepts current fixtures from `common/test-fixtures/`.
- [ ] `argus plugin` manifest keys align with `common/manifest/argus-plugin.schema.json`.
- [ ] `sdk/config` compatibility metadata updated for any version policy changes.

## Engine package quality

- [ ] Unity package imports into clean project and sample scene runs.
- [ ] Unreal plugin compiles runtime/editor modules on supported UE version.
- [ ] Godot addon enables cleanly and heartbeat event succeeds.
- [ ] Test-mode symbols are stripped/disabled in shipping builds.

## Operations and support

- [ ] Dashboard shows SDK heartbeat and health diagnostics for test projects.
- [ ] Error taxonomy in support docs matches backend error codes.
- [ ] Changelog / release notes updated.
- [ ] Escalation template version bumped.
