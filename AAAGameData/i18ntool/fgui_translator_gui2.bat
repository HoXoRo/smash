@echo off
REM 这是一个批处理文件，用于执行 fgui_translator_gui2.py

echo 正在启动 fgui_translator_gui2.py...
python fgui_translator_gui2.py

REM 如果上面的命令执行失败，尝试使用 python3
if errorlevel 1 (
    echo 使用 python 执行失败，尝试使用 python3...
    python3 fgui_translator_gui2.py
)

if errorlevel 1 (
    echo 无法启动脚本，请确保:
    echo 1. Python 已安装并添加到系统路径
    echo 2. fgui_translator_gui2.py 文件存在于当前目录
    pause
)

pause