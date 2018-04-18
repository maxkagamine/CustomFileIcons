@echo off
setlocal enabledelayedexpansion

if "%~1" == "" (
  echo Drop one or more svgs onto the script to generate an ico.
  timeout /t 4
  exit /b
)

cd /D "%~dp0"

for %%f in (%*) do (

  set "defaultname=%%~nf"
  set "defaultname=!defaultname:file_type_=!"
  set "name="
  set /p name="Name [!defaultname!]: "
  if "!name!" == "" set "name=!defaultname!"
  echo.

  CustomFileIcons.Generator\bin\Release\CustomFileIcons.Generator.exe --hash "%%~f" "icons\!name!.ico"

  if not !errorlevel! == 0 (
    goto err
  )

  echo.

)

timeout /t 2 > nul
exit /b

:err
echo.
pause
