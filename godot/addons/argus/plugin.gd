@tool
extends EditorPlugin

func _enter_tree() -> void:
    add_autoload_singleton("ArgusRuntime", "res://addons/argus/runtime.gd")

func _exit_tree() -> void:
    remove_autoload_singleton("ArgusRuntime")
