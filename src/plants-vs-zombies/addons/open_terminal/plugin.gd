@tool
extends EditorPlugin

## 一键在项目目录打开 Windows Terminal（pwsh）。
##
## 不用插件内嵌终端：编辑器不会替自绘控件打开输入法，中文输入、字形对齐这些都要自己补，
## 而 Windows Terminal 本体这些全都是现成的。代价是独立窗口，换来的是不折腾也不卡死。

## wt.exe 查找顺序：先 PATH（Store 版或手加过 PATH），再便携版常见位置。
## 换了 WT 版本就把新路径加到列表里。
const WT_CANDIDATES := [
	"wt.exe",
	"C:/Users/HYTomZ/Downloads/Microsoft.WindowsTerminal_1.24.10921.0_x64/terminal-1.24.10921.0/wt.exe",
	"C:/Users/HYTomZ/Downloads/terminal-1.24.10921.0/wt.exe",
]

const SHELL_EXE := "pwsh.exe"

## 向上查找的最大层数，防止路径异常时死循环
const MAX_UPWARD_LEVELS := 32

var _button: Button = null

func _enter_tree() -> void:
	_button = Button.new()
	_button.text = ">_"
	_button.flat = true
	_button.focus_mode = Control.FOCUS_NONE
	_button.tooltip_text = "在项目目录打开 Windows Terminal (pwsh)"
	_button.pressed.connect(_launch)
	add_control_to_container(EditorPlugin.CONTAINER_TOOLBAR, _button)

func _exit_tree() -> void:
	if is_instance_valid(_button):
		remove_control_from_container(EditorPlugin.CONTAINER_TOOLBAR, _button)
		_button.queue_free()
		_button = null

func _launch() -> void:
	var wt := _resolve_wt()
	if wt.is_empty():
		push_error("未找到 Windows Terminal (wt.exe)。把实际路径填进 addons/open_terminal/plugin.gd 的 WT_CANDIDATES。")
		return

	var cwd := _resolve_working_dir()
	# -w 0 复用最近一个 WT 窗口开新标签，没有就新建，避免每点一次堆一个窗口
	var err := OS.create_process(wt, ["-w", "0", "nt", "-d", cwd, SHELL_EXE])
	if err != OK:
		push_error("启动 Windows Terminal 失败：%s" % error_string(err))

## 选择终端的工作目录，按优先级逐轮向上找：
##   1. 最近的 .git 所在目录（仓库根，git / dotnet 命令都在这一层）
##   2. 最近的 .sln 所在目录（非 git 管理的解决方案）
##   3. 回退到 Godot 项目根（.godot 所在）
## 两轮是分开的：.git 可能比 .sln 更靠上，这时优先取更上层的仓库根。
func _resolve_working_dir() -> String:
	var start := ProjectSettings.globalize_path("res://").trim_suffix("/")

	var root := _find_upwards(start, _is_repo_root)
	if root.is_empty():
		root = _find_upwards(start, _is_solution_dir)
	if root.is_empty():
		root = start
	return root

func _is_repo_root(dir: String) -> bool:
	# .git 可能是目录（普通仓库）也可能是文件（worktree / submodule）
	var git := dir.path_join(".git")
	return DirAccess.dir_exists_absolute(git) or FileAccess.file_exists(git)

func _is_solution_dir(dir: String) -> bool:
	var da := DirAccess.open(dir)
	if da == null:
		return false
	for file_name in da.get_files():
		if file_name.get_extension().to_lower() == "sln":
			return true
	return false

func _find_upwards(start: String, predicate: Callable) -> String:
	var dir := start
	for _i in range(MAX_UPWARD_LEVELS):
		if predicate.call(dir):
			return dir
		var parent := dir.get_base_dir()
		if parent.is_empty() or parent == dir:
			break
		dir = parent
	return ""

func _resolve_wt() -> String:
	for path in WT_CANDIDATES:
		if path == "wt.exe":
			var out: Array = []
			if OS.execute("where", ["wt.exe"], out, true, false) == 0 and not out.is_empty():
				return "wt.exe"
		elif FileAccess.file_exists(path):
			return path
	return ""
