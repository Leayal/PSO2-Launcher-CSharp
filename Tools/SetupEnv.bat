@echo off
setlocal
SET "DOTNET_CLI_TELEMETRY_OPTOUT=1"
cd /d %~dp0
powershell.exe -ExecutionPolicy Bypass -File "DownloadDotnetSDK.ps1"
endlocal