@echo off

CustomFileIcons\bin\Release\CustomFileIcons.exe

if %errorlevel% == 0 (
  timeout /t 2 > nul
) else (
  echo.
  pause
)
