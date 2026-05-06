extends Node

const SCHEMA_VERSION := "1.0.0"
const SDK_NAME := "argus-godot"
const SDK_VERSION := "0.1.0"

func build_envelope(event_type: String, payload: Dictionary, project_id: String, session_id: String) -> Dictionary:
    return {
        "schema_version": SCHEMA_VERSION,
        "sdk_name": SDK_NAME,
        "sdk_version": SDK_VERSION,
        "engine": "godot",
        "engine_version": Engine.get_version_info().get("string", "unknown"),
        "platform": OS.get_name().to_lower(),
        "project_id": project_id,
        "session_id": session_id,
        "privacy_mode": "live",
        "event_type": event_type,
        "payload": payload
    }
