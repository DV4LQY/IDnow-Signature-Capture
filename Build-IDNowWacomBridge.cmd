@echo off
setlocal
set "MSBUILD=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

if not exist "%MSBUILD%" (
  echo .NET Framework 4 MSBuild was not found.
  exit /b 1
)

"%MSBUILD%" "%~dp0IDNowWacomBridge.csproj" /t:Rebuild /p:Configuration=Release /p:Platform=x86 /v:minimal
exit /b %ERRORLEVEL%
