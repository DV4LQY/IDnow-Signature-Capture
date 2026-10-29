@echo off
setlocal
set "BRIDGE_EXE=%~dp0bin\Release\IDNowWacomBridge.exe"

if not exist "%BRIDGE_EXE%" (
  echo The bridge has not been built yet.
  echo Build it with Build-IDNowWacomBridge.cmd.
  exit /b 1
)

start "IDNow Wacom STU-430 Bridge" "%BRIDGE_EXE%"
