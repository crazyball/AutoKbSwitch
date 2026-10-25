@echo off
cd /d "%~dp0"
echo Building KbSwitch...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
if errorlevel 1 (
  echo.
  echo *** Build failed: see the error above. ***
  pause
  exit /b 1
)
echo.
echo Done: publish\KbSwitch.exe
explorer publish
pause
