@echo off
setlocal
cd /d %~dp0
powershell.exe -ExecutionPolicy Bypass -File "DownloadDotnetSDK.ps1"
endlocal