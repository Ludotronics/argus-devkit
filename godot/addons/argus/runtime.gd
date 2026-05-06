extends Node

const SCHEMA_VERSION := "1.0.0"
const SDK_NAME := "argus-godot"
const SDK_VERSION := "0.1.0"

var api_key := ""
var project_id := ""
var backend_url := "https://api.argus.ludotronics.io"
var mode := "off"
var session_id := ""
var consent_granted := false
var enabled := false
var _queue: Array[Dictionary] = []
var _flags: Dictionary = {}

func init(config: Dictionary) -> void:
    api_key = config.get("api_key", OS.get_environment("ARGUS_API_KEY"))
    project_id = config.get("project_id", OS.get_environment("ARGUS_PROJECT_ID"))
    backend_url = config.get("backend_url", OS.get_environment("ARGUS_BASE_URL", "https://api.argus.ludotronics.io")).rstrip("/")
    mode = config.get("mode", OS.get_environment("ARGUS_MODE", "live")).to_lower()
    session_id = config.get("session_id", str(Time.get_unix_time_from_system()) + "-" + str(randi()))
    consent_granted = bool(config.get("consent_granted", mode == "test"))
    enabled = mode != "off" and project_id != "" and api_key != ""
    if enabled:
        health("initialized")

func event(name: String, properties: Dictionary = {}) -> void:
    if not _can_send():
        return
    _enqueue(build_envelope("event", {"name": name, "properties": properties, "ts": Time.get_unix_time_from_system()}, project_id, session_id))

func metric(name: String, value: float, tags: Dictionary = {}) -> void:
    if not _can_send():
        return
    _enqueue(build_envelope("metric", {"name": name, "value": value, "tags": tags, "ts": Time.get_unix_time_from_system()}, project_id, session_id))

func perf(fields: Dictionary) -> void:
    metric("perf", float(fields.get("fps", Engine.get_frames_per_second())), fields)

func crash(message: String, fields: Dictionary = {}) -> void:
    event("crash", {"message": message, "fields": fields})

func set_mode(next_mode: String) -> void:
    mode = next_mode.to_lower()
    enabled = mode != "off"

func set_consent(granted: bool) -> void:
    consent_granted = granted

func apply_config(config: Dictionary) -> void:
    if config.get("enabled", true) == false or config.get("kill_switch", false) == true:
        enabled = false
    _flags = config.get("flags", {})

func flag(key: String, fallback = null):
    return _flags.get(key, fallback)

func health(status: String, fields: Dictionary = {}) -> void:
    if mode == "off":
        return
    _enqueue(build_envelope("sdk_health", {"status": status, "queue_depth": _queue.size(), "fields": fields}, project_id, session_id))

func capture_state() -> Dictionary:
    var tree := get_tree()
    var scene := ""
    if tree and tree.current_scene:
        scene = tree.current_scene.name
    return {
        "scene": scene,
        "state_hash": state_hash(),
        "fps": Engine.get_frames_per_second(),
        "runtime": runtime_metadata()
    }

func accept_command(command: Dictionary) -> Dictionary:
    return {"acknowledged": true, "action": command.get("action", "unknown"), "state_hash": state_hash()}

func state_hash() -> String:
    var payload := JSON.stringify(capture_state_shallow())
    return str(payload.hash())

func runtime_metadata() -> Dictionary:
    return {
        "engine": "godot",
        "engine_version": Engine.get_version_info().get("string", "unknown"),
        "platform": OS.get_name().to_lower(),
        "sdk_schema_version": SCHEMA_VERSION,
        "sdk_mode": mode
    }

func build_envelope(event_type: String, payload: Dictionary, project_id_value: String, session_id_value: String) -> Dictionary:
    return {
        "schema_version": SCHEMA_VERSION,
        "sdk_name": SDK_NAME,
        "sdk_version": SDK_VERSION,
        "engine": "godot",
        "engine_version": Engine.get_version_info().get("string", "unknown"),
        "platform": OS.get_name().to_lower(),
        "project_id": project_id_value,
        "session_id": session_id_value,
        "privacy_mode": mode,
        "event_type": event_type,
        "payload": payload
    }

func flush() -> Array[Dictionary]:
    var batch := _queue.duplicate()
    _queue.clear()
    return batch

func _can_send() -> bool:
    return enabled and (mode == "test" or consent_granted)

func _enqueue(envelope: Dictionary) -> void:
    if _queue.size() >= 500:
        _queue.pop_front()
    _queue.append(envelope)

func capture_state_shallow() -> Dictionary:
    var tree := get_tree()
    var scene := ""
    if tree and tree.current_scene:
        scene = tree.current_scene.name
    return {"scene": scene, "fps": Engine.get_frames_per_second()}
